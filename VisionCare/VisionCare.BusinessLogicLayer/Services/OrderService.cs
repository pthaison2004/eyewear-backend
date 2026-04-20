using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.Order;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class OrderService : IOrderService
{
    private readonly VisionCareContext _context;

    public OrderService(VisionCareContext context)
    {
        _context = context;
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

        bool hasPreOrderItems = false;
        decimal paidAmount = 0;
        decimal productTotal = 0;

        foreach (var item in cart.CartItems)
        {
            if (item.Variant == null)
            {
                throw new InvalidOperationException("Cart contains an item with no variant.");
            }

            var unitPrice = item.Variant.Product.BasePrice + (item.Variant.AdditionalPrice ?? 0);
            var itemTotal = unitPrice * item.Quantity;
            productTotal += itemTotal;

            if (item.Variant.StockQuantity < item.Quantity)
            {
                // Đây là sản phẩm nợ hàng (Pre-order)
                hasPreOrderItems = true;
                paidAmount += itemTotal * 0.3m; // Trả trước 30%
            }
            else
            {
                // Sản phẩm có sẵn
                paidAmount += itemTotal; // Trả 100%
            }
        }

        // Tính phí ship (ví dụ cố định 30.000 nếu đơn dưới 2 triệu)
        decimal shippingFee = productTotal >= 2000000 ? 0 : 30000;
        var totalAmount = productTotal + shippingFee;

        var order = new Order
        {
            CustomerId = customerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = totalAmount,
            PaidAmount = paidAmount,
            OrderStatus = "Pending",
            PaymentStatus = "PartiallyPaid",
            OrderType = hasPreOrderItems ? "PreOrder" : request.OrderType,
            ShippingAddress = request.ShippingAddress,
            PreOrderDeadline = hasPreOrderItems ? DateTime.UtcNow.AddDays(15) : null
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        foreach (var cartItem in cart.CartItems)
        {
            var unitPrice = cartItem.Variant.Product.BasePrice + (cartItem.Variant.AdditionalPrice ?? 0);

            var orderItem = new OrderItem
            {
                OrderId = order.OrderId,
                VariantId = cartItem.VariantId,
                PrescriptionId = cartItem.PrescriptionId,
                Quantity = cartItem.Quantity,
                UnitPrice = unitPrice
            };

            _context.OrderItems.Add(orderItem);

            // Chỉ trừ kho nếu là hàng có sẵn
            if (cartItem.Variant.StockQuantity >= cartItem.Quantity)
            {
                cartItem.Variant.StockQuantity -= cartItem.Quantity;
            }
        }

        await _context.SaveChangesAsync();

        _context.CartItems.RemoveRange(cart.CartItems);
        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();

        var orderItems = await _context.OrderItems
            .Where(oi => oi.OrderId == order.OrderId)
            .Include(oi => oi.Variant)
            .ThenInclude(v => v.Product)
            .ToListAsync();

        return MapToOrderResponseDto(order, orderItems);
    }

    public async Task<List<OrderListItemDto>> GetOrdersAsync(int customerId)
    {
        var orders = await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return orders.Select(o => new OrderListItemDto
        {
            OrderId = o.OrderId,
            OrderDate = o.OrderDate ?? DateTime.MinValue,
            TotalAmount = o.TotalAmount,
            OrderStatus = o.OrderStatus ?? string.Empty,
            PaymentStatus = o.PaymentStatus ?? string.Empty,
            OrderType = o.OrderType ?? string.Empty,
            PaidAmount = o.PaidAmount,
            PreOrderDeadline = o.PreOrderDeadline,
            ItemCount = o.OrderItems.Count,
            Items = o.OrderItems.Select(i => new OrderItemDto
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
        }).ToList();
    }

    public async Task<OrderResponseDto> GetOrderByIdAsync(int orderId, int customerId)
    {
        var order = await _context.Orders
            .Where(o => o.OrderId == orderId && o.CustomerId == customerId)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync();

        if (order == null)
        {
            throw new InvalidOperationException("Order not found");
        }

        return MapToOrderResponseDto(order, order.OrderItems.ToList());
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

    private static OrderResponseDto MapToOrderResponseDto(Order order, List<OrderItem> items)
    {
        return new OrderResponseDto
        {
            OrderId = order.OrderId,
            CustomerId = order.CustomerId ?? 0,
            OrderDate = order.OrderDate ?? DateTime.MinValue,
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus ?? string.Empty,
            PaymentStatus = order.PaymentStatus ?? string.Empty,
            OrderType = order.OrderType ?? string.Empty,
            PaidAmount = order.PaidAmount,
            PreOrderDeadline = order.PreOrderDeadline,
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
}
