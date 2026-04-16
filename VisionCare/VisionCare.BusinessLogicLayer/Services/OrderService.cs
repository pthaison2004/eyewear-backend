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

        foreach (var item in cart.CartItems)
        {
            if (item.Variant.StockQuantity < item.Quantity)
            {
                throw new InvalidOperationException($"Not enough stock for {item.Variant.Product.ProductName}");
            }
        }

        var totalAmount = cart.CartItems.Sum(item =>
        {
            var unitPrice = item.Variant.Product.BasePrice + (item.Variant.AdditionalPrice ?? 0);
            return unitPrice * item.Quantity;
        });

        var order = new Order
        {
            CustomerId = customerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = totalAmount,
            OrderStatus = "Pending",
            PaymentStatus = "Unpaid",
            OrderType = request.OrderType,
            ShippingAddress = request.ShippingAddress
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

            cartItem.Variant.StockQuantity -= cartItem.Quantity;
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
            ItemCount = o.OrderItems.Count
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
            item.Variant.StockQuantity += item.Quantity;
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
            ShippingAddress = order.ShippingAddress,
            TrackingNumber = order.TrackingNumber,
            Items = items.Select(i => new OrderItemDto
            {
                OrderItemId = i.OrderItemId,
                VariantId = i.VariantId ?? 0,
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
