using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.OpsOrder;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class OpsOrderService : IOpsOrderService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "Confirmed", "Processing", "Packed", "Shipped", "Delivered", "Cancelled"
    };

    private static readonly HashSet<string> PackableStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "Confirmed", "Processing"
    };

    private readonly VisionCareContext _context;

    public OpsOrderService(VisionCareContext context)
    {
        _context = context;
    }

    public async Task<OrderOpsDetailDto> PackOrderAsync(int orderId, int staffId)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }

        if (!PackableStatuses.Contains(order.OrderStatus ?? string.Empty))
        {
            throw new InvalidOperationException(
                $"Order cannot be packed. Current status is '{order.OrderStatus}'. Only orders with status 'Pending', 'Confirmed', or 'Processing' can be packed.");
        }

        order.PackedAt = DateTime.UtcNow;
        order.PackedBy = staffId;
        order.OrderStatus = "Packed";

        await _context.SaveChangesAsync();

        return MapToOrderOpsDetailDto(order);
    }

    public async Task<OrderOpsDetailDto> UpdateOrderStatusAsync(int orderId, int staffId, UpdateOrderStatusRequestDto request)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }

        var newStatus = request.Status?.Trim();
        if (string.IsNullOrEmpty(newStatus) || !ValidStatuses.Contains(newStatus))
        {
            throw new ArgumentException(
                $"Invalid status '{request.Status}'. Valid statuses are: {string.Join(", ", ValidStatuses)}.");
        }

        order.OrderStatus = newStatus;

        if (string.Equals(newStatus, "Packed", StringComparison.OrdinalIgnoreCase))
        {
            order.PackedAt = DateTime.UtcNow;
            order.PackedBy = staffId;
        }

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            var existingNote = order.StaffNote ?? string.Empty;
            var noteEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] [{staffId}] {request.Note}";
            order.StaffNote = string.IsNullOrEmpty(existingNote)
                ? noteEntry
                : $"{existingNote}\n{noteEntry}";
        }

        await _context.SaveChangesAsync();

        return MapToOrderOpsDetailDto(order);
    }

    private static OrderOpsDetailDto MapToOrderOpsDetailDto(Order order)
    {
        return new OrderOpsDetailDto
        {
            OrderId = order.OrderId,
            OrderCode = $"ORD-{order.OrderId:D4}",
            CustomerName = order.Customer?.FullName ?? string.Empty,
            CustomerEmail = order.Customer?.Email ?? string.Empty,
            OrderType = order.OrderType ?? string.Empty,
            OrderStatus = order.OrderStatus ?? string.Empty,
            PaymentStatus = order.PaymentStatus ?? string.Empty,
            TotalAmount = order.TotalAmount,
            ShippingAddress = order.ShippingAddress ?? string.Empty,
            OrderDate = order.OrderDate ?? DateTime.MinValue,
            PackedAt = order.PackedAt,
            PackedBy = order.PackedBy,
            StaffNote = order.StaffNote ?? string.Empty,
            Items = order.OrderItems.Select(i => new OrderItemOpsDto
            {
                OrderItemId = i.OrderItemId,
                VariantId = i.VariantId,
                ProductName = i.Variant?.Product?.ProductName ?? string.Empty,
                VariantInfo = FormatVariantInfo(i.Variant),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }

    private static string FormatVariantInfo(ProductVariant? variant)
    {
        if (variant == null) return string.Empty;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(variant.Color)) parts.Add(variant.Color);
        if (!string.IsNullOrWhiteSpace(variant.Size)) parts.Add(variant.Size);
        return string.Join(" / ", parts);
    }
}
