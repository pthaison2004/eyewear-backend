using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.Order;
using VisionCare.DataAccessLayer.Models;
using PayOS;
using Microsoft.Extensions.Configuration;

namespace VisionCare.BusinessLogicLayer.Services;

public class OrderService : IOrderService
{
    private readonly VisionCareContext _context;
    private readonly PayOSClient _payOS;
    private readonly IConfiguration _configuration;

    public OrderService(VisionCareContext context, PayOSClient payOS, IConfiguration configuration)
    {
        _context = context;
        _payOS = payOS;
        _configuration = configuration;
    }

    public async Task<OrderResponseDto> CreateOrderAsync(int customerId, CreateOrderRequestDto request)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Variant)
            .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (cart == null || !cart.CartItems.Any())
        {
            throw new InvalidOperationException("Cart is empty");
        }

        if (string.Equals(request.OrderType, "Pre-order", StringComparison.OrdinalIgnoreCase))
        {
            return await CreatePreOrderReservationsAsync(customerId, request, cart);
        }

        foreach (var item in cart.CartItems)
        {
            if (item.Variant == null)
            {
                throw new InvalidOperationException("Cart contains an item with no variant.");
            }
            if (item.Variant.StockQuantity < item.Quantity)
            {
                throw new InvalidOperationException($"Not enough stock for {item.Variant.Product?.ProductName ?? "Unknown Product"}");
            }
        }

        var totalAmount = cart.CartItems.Sum(item =>
        {
            var unitPrice = (item.Variant?.Product?.BasePrice ?? 0) + (item.Variant?.AdditionalPrice ?? 0);
            return unitPrice * item.Quantity;
        });

        var orderType = cart.CartItems.Any(item => item.PrescriptionId.HasValue)
            ? "Prescription"
            : request.OrderType;

        var order = new Order
        {
            CustomerId = customerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = totalAmount,
            OrderStatus = "Pending",
            PaymentStatus = "Unpaid",
            OrderType = orderType,
            ShippingAddress = request.ShippingAddress
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        foreach (var cartItem in cart.CartItems)
        {
            var unitPrice = (cartItem.Variant?.Product?.BasePrice ?? 0) + (cartItem.Variant?.AdditionalPrice ?? 0);

            var orderItem = new OrderItem
            {
                OrderId = order.OrderId,
                VariantId = cartItem.VariantId,
                PrescriptionId = cartItem.PrescriptionId,
                Quantity = cartItem.Quantity,
                UnitPrice = unitPrice
            };

            _context.OrderItems.Add(orderItem);

            cartItem.Variant.StockQuantity -= cartItem.Quantity;
        }

        await _context.SaveChangesAsync();

        _context.CartItems.RemoveRange(cart.CartItems);
        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();

        var orderItems = await _context.OrderItems
            .Where(oi => oi.OrderId == order.OrderId)
            .Include(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .ToListAsync();

        return MapToOrderResponseDto(order, orderItems);
    }

    private async Task<OrderResponseDto> CreatePreOrderReservationsAsync(int customerId, CreateOrderRequestDto request, Cart cart)
    {
        var now = DateTime.UtcNow;
        var reservationIds = new List<int>();
        decimal totalAmount = 0;

        foreach (var item in cart.CartItems)
        {
            if (item.Variant?.Product == null)
            {
                throw new InvalidOperationException("Cart contains an item with no product variant.");
            }

            var isPreOrderEligible = item.Variant.Product.IsPreOrder == true || (item.Variant.StockQuantity ?? 0) <= 0;
            if (!isPreOrderEligible)
            {
                throw new InvalidOperationException($"{item.Variant.Product.ProductName} is not available for pre-order.");
            }

            var campaignProduct = await _context.PreOrderCampaignProducts
                .Include(cp => cp.Campaign)
                .Where(cp =>
                    cp.Campaign != null &&
                    cp.ProductId == item.Variant.ProductId &&
                    (cp.VariantId == null || cp.VariantId == item.VariantId) &&
                    cp.Campaign.EndDate >= now &&
                    cp.Campaign.Status.ToLower() != "closed" &&
                    cp.Campaign.Status.ToLower() != "cancelled" &&
                    cp.Campaign.Status.ToLower() != "fulfilled")
                .OrderByDescending(cp => cp.VariantId == item.VariantId)
                .ThenBy(cp => cp.Campaign!.EndDate)
                .FirstOrDefaultAsync();

            if (campaignProduct?.Campaign == null)
            {
                throw new InvalidOperationException($"No active pre-order campaign found for {item.Variant.Product.ProductName}.");
            }

            var campaign = campaignProduct.Campaign;
            var alreadyReservedByCustomer = await _context.PreOrderReservations
                .Where(r =>
                    r.CampaignId == campaign.CampaignId &&
                    r.CustomerId == customerId &&
                    r.Status.ToLower() != "cancelled")
                .SumAsync(r => (int?)r.ReservedQuantity) ?? 0;

            if (alreadyReservedByCustomer + item.Quantity > campaign.MaxPerCustomer)
            {
                throw new InvalidOperationException($"Pre-order limit for {campaign.CampaignName} is {campaign.MaxPerCustomer} items per customer.");
            }

            if (campaign.MaxQuantity.HasValue && campaign.CurrentReserved + item.Quantity > campaign.MaxQuantity.Value)
            {
                throw new InvalidOperationException($"Pre-order campaign {campaign.CampaignName} does not have enough reservation slots.");
            }

            var unitPrice = campaignProduct.CampaignPrice > 0
                ? campaignProduct.CampaignPrice
                : item.Variant.Product.BasePrice + (item.Variant.AdditionalPrice ?? 0);

            var reservation = new PreOrderReservation
            {
                ReservationCode = $"RES-{now:yyyyMMddHHmmss}-{item.VariantId}",
                CampaignId = campaign.CampaignId,
                CustomerId = customerId,
                VariantId = item.VariantId,
                ReservedQuantity = item.Quantity,
                UnitPrice = unitPrice,
                ShippingAddress = request.ShippingAddress,
                Status = "reserved",
                ExpiresAt = now.AddDays(3),
                CreatedAt = now
            };

            _context.PreOrderReservations.Add(reservation);
            campaign.CurrentReserved += item.Quantity;
            campaign.UpdatedAt = now;
            campaignProduct.ReservedQuantity += item.Quantity;
            totalAmount += unitPrice * item.Quantity;

            await _context.SaveChangesAsync();
            reservationIds.Add(reservation.ReservationId);
        }

        _context.CartItems.RemoveRange(cart.CartItems);
        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();

        return new OrderResponseDto
        {
            OrderId = 0,
            CustomerId = customerId,
            OrderDate = now,
            TotalAmount = totalAmount,
            OrderStatus = "Reserved",
            PaymentStatus = "Unpaid",
            OrderType = "Pre-order",
            PaidAmount = 0,
            ShippingAddress = request.ShippingAddress,
            ReservationId = reservationIds.FirstOrDefault(),
            PreOrderReservationIds = reservationIds,
            Items = new()
        };
    }

    public async Task<List<OrderListItemDto>> GetOrdersAsync(int customerId)
    {
        var orders = await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var reservations = await _context.PreOrderReservations
            .Where(r => r.CustomerId == customerId && r.ConvertedOrderId == null)
            .Include(r => r.Variant)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var orderItems = orders.Select(o => new OrderListItemDto
        {
            OrderId = o.OrderId,
            OrderDate = o.OrderDate ?? DateTime.MinValue,
            TotalAmount = o.TotalAmount,
            OrderStatus = o.OrderStatus ?? string.Empty,
            PaymentStatus = o.PaymentStatus ?? string.Empty,
            OrderType = o.OrderType ?? string.Empty,
            ItemCount = o.OrderItems.Count
        }).ToList();

        var reservationItems = reservations.Select(r => new OrderListItemDto
        {
            OrderId = r.ReservationId, // Using ReservationId as OrderId for tracking
            OrderDate = r.CreatedAt,
            TotalAmount = r.UnitPrice * r.ReservedQuantity,
            OrderStatus = r.Status, // reserved, paid, etc.
            PaymentStatus = r.Status.ToLower() == "paid" ? "Paid" : (r.PaidAt != null ? "Đã đặt cọc 30%" : "Unpaid"),
            OrderType = "Pre-order",
            ItemCount = 1
        }).ToList();

        return orderItems.Concat(reservationItems)
            .OrderByDescending(x => x.OrderDate)
            .ToList();
    }

    public async Task<OrderResponseDto> GetOrderByIdAsync(int orderId, int customerId)
    {
        var order = await _context.Orders
            .Where(o => o.OrderId == orderId && o.CustomerId == customerId)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync();

        if (order != null)
        {
            return MapToOrderResponseDto(order, order.OrderItems.ToList());
        }

        // Check if it's a reservation instead
        var reservation = await _context.PreOrderReservations
            .Include(r => r.Variant)
            .ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync(r => r.ReservationId == orderId && r.CustomerId == customerId);

        if (reservation == null)
        {
            throw new InvalidOperationException("Order or Reservation not found");
        }

        var totalAmount = reservation.UnitPrice * reservation.ReservedQuantity;
        var paidAmount = reservation.Status.ToLower() == "paid" ? (totalAmount * 0.3m) : 0;

        return new OrderResponseDto
        {
            OrderId = reservation.ReservationId,
            CustomerId = customerId,
            OrderDate = reservation.CreatedAt,
            TotalAmount = totalAmount,
            PaidAmount = paidAmount,
            OrderStatus = reservation.Status,
            PaymentStatus = reservation.Status.ToLower() == "paid" ? "Paid" : (reservation.PaidAt != null ? "Đã đặt cọc 30%" : "Unpaid"),
            OrderType = "Pre-order",
            ShippingAddress = reservation.ShippingAddress,
            Items = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    OrderItemId = reservation.ReservationId,
                    VariantId = reservation.VariantId,
                    ProductName = reservation.Variant?.Product?.ProductName ?? "Sản phẩm đặt trước",
                    VariantColor = reservation.Variant?.Color,
                    VariantSize = reservation.Variant?.Size,
                    Sku = reservation.Variant?.Sku,
                    Quantity = reservation.ReservedQuantity,
                    UnitPrice = reservation.UnitPrice,
                    Subtotal = reservation.UnitPrice * reservation.ReservedQuantity
                }
            }
        };
    }

    public async Task<OrderResponseDto> CancelOrderAsync(int orderId, int customerId)
    {
        var order = await _context.Orders
            .Where(o => o.OrderId == orderId && o.CustomerId == customerId)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .FirstOrDefaultAsync();

        if (order == null)
        {
            throw new InvalidOperationException("Order not found");
        }

        if (order.OrderStatus != "Pending")
        {
            throw new InvalidOperationException("Only pending orders can be cancelled");
        }

        foreach (var item in order.OrderItems)
        {
            if (item.Variant != null)
            {
                item.Variant.StockQuantity += item.Quantity;
            }
        }

        order.OrderStatus = "Cancelled";
        await _context.SaveChangesAsync();

        return MapToOrderResponseDto(order, order.OrderItems.ToList());
    }

    public async Task<OrderResponseDto> CompleteOrderAsync(int orderId, int customerId)
    {
        var order = await _context.Orders
            .Where(o => o.OrderId == orderId && o.CustomerId == customerId)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .Include(o => o.ShippingOrders)
            .FirstOrDefaultAsync();

        if (order == null)
        {
            throw new InvalidOperationException("Order not found");
        }

        if (!string.Equals(order.OrderStatus, "Shipped", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only shipped orders can be confirmed as received");
        }

        var fromStatus = order.OrderStatus ?? string.Empty;
        order.OrderStatus = "Delivered";

        var latestShippingOrder = order.ShippingOrders
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault();

        if (latestShippingOrder != null)
        {
            var fromShippingStatusId = latestShippingOrder.ShippingStatusId;
            latestShippingOrder.ShippingStatusId = 5;
            latestShippingOrder.DeliveredAt = DateTime.UtcNow;
            latestShippingOrder.CarrierStatus = "Delivered";

            _context.ShippingStatusHistories.Add(new ShippingStatusHistory
            {
                ShippingOrderId = latestShippingOrder.ShippingOrderId,
                FromStatusId = fromShippingStatusId,
                ToStatusId = 5,
                CarrierStatusText = "Customer confirmed receipt",
                UpdatedBy = customerId,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            FromStatus = fromStatus,
            ToStatus = "Delivered",
            Note = "Customer confirmed receipt",
            ChangedBy = customerId,
            ChangedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return MapToOrderResponseDto(order, order.OrderItems.ToList());
    }

    private static OrderResponseDto MapToOrderResponseDto(Order order, List<OrderItem> items)
    {
        return new OrderResponseDto
        {
            OrderId = order.OrderId,
            CustomerId = order.CustomerId ?? 0,
            OrderDate = order.OrderDate ?? DateTime.MinValue,
            TotalAmount = order.TotalAmount,
            PaidAmount = order.PaidAmount ?? 0,
            OrderStatus = order.OrderStatus ?? string.Empty,
            PaymentStatus = order.PaymentStatus ?? string.Empty,
            OrderType = order.OrderType ?? string.Empty,
            ShippingAddress = order.ShippingAddress,
            TrackingNumber = order.TrackingNumber,
            Items = items.Select(i => new OrderItemDto
            {
                OrderItemId = i.OrderItemId,
                VariantId = i.VariantId,
                ProductName = i.Variant?.Product?.ProductName ?? string.Empty,
                VariantColor = i.Variant?.Color,
                VariantSize = i.Variant?.Size,
                Sku = i.Variant?.Sku,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.UnitPrice * i.Quantity,
                PrescriptionId = i.PrescriptionId
            }).ToList()
        };
    }

    public async Task<PayOSLinkResponseDto> CreatePaymentLinkAsync(int orderId, int customerId)
    {
        var order = await _context.Orders
            .Where(o => o.OrderId == orderId && o.CustomerId == customerId)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync();

        if (order == null)
        {
            throw new InvalidOperationException("Order not found or does not belong to the user.");
        }

        if (order.PaymentStatus == "Paid")
        {
            throw new InvalidOperationException("Order is already paid.");
        }

        if (order.OrderStatus == "Cancelled")
        {
            throw new InvalidOperationException("Order is cancelled.");
        }

        var domain = _configuration["PayOS:ReturnUrl"] ?? "http://localhost:5173/payment/success";
        var cancelDomain = _configuration["PayOS:CancelUrl"] ?? "http://localhost:5173/payment/cancel";

        // orderCode must be less than 9007199254740991. Let's use a combination of orderId and timestamp
        // to avoid duplicating orderCode for PayOS
        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long orderCode = long.Parse($"{orderId}{currentTimestamp}");

        decimal remainingAmount = order.TotalAmount;
        if (order.OrderType == "Pre-order" && order.PaymentStatus == "PartialPaid")
        {
            var reservation = await _context.PreOrderReservations
                .Include(r => r.Campaign)
                .FirstOrDefaultAsync(r => r.ConvertedOrderId == order.OrderId);
                
            if (reservation != null && reservation.Campaign != null)
            {
                var depositRatio = reservation.Campaign.DepositRatio ?? 0m;
                var depositAmount = order.TotalAmount * depositRatio;
                remainingAmount = order.TotalAmount - depositAmount;
            }
        }

        var totalAmount = Convert.ToInt64(Math.Round(remainingAmount));
        
        var items = new List<PayOS.Models.V2.PaymentRequests.PaymentLinkItem>
        {
            new PayOS.Models.V2.PaymentRequests.PaymentLinkItem
            {
                Name = "Gong Kinh",
                Quantity = 1,
                Price = Convert.ToInt32(Math.Round(remainingAmount))
            }
        };

        var paymentData = new PayOS.Models.V2.PaymentRequests.CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = totalAmount,
            Description = $"Order {order.OrderId}".Substring(0, Math.Min($"Order {order.OrderId}".Length, 25)),
            CancelUrl = $"{cancelDomain}?orderId={order.OrderId}",
            ReturnUrl = $"{domain}?orderId={order.OrderId}"
        };

        var createPayment = await _payOS.PaymentRequests.CreateAsync(paymentData);

        return new PayOSLinkResponseDto
        {
            CheckoutUrl = createPayment.CheckoutUrl,
            PaymentLinkId = createPayment.PaymentLinkId
        };
    }

    public async Task<bool> CheckPaymentStatusAsync(int orderId, int customerId, string paymentLinkId)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId);

        if (order == null)
        {
            throw new InvalidOperationException("Order not found or does not belong to the user.");
        }

        if (order.PaymentStatus == "Paid")
        {
            return true;
        }

        var paymentInfo = await _payOS.PaymentRequests.GetAsync(paymentLinkId);
        if (paymentInfo != null && paymentInfo.Status.ToString().Equals("PAID", StringComparison.OrdinalIgnoreCase))
        {
            order.PaymentStatus = "Paid";
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> SimulatePaymentSuccessAsync(int orderId, int customerId)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId);

        if (order == null)
        {
            throw new KeyNotFoundException("Order not found or does not belong to the user.");
        }

        if (order.PaymentStatus == "Paid")
        {
            return true;
        }

        order.PaymentStatus = "Paid";
        order.PaidAmount = order.TotalAmount;

        await _context.SaveChangesAsync();
        return true;
    }
}
