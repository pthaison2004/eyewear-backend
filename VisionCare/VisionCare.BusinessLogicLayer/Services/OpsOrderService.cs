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

    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cancelled", "Delivered"
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

        return await UpdateOrderStatusAsync(orderId, staffId, new UpdateOrderStatusRequestDto
        {
            Status = "Packed",
            Note = $"Order packed by staff ID {staffId}."
        });
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

        var currentStatus = order.OrderStatus ?? string.Empty;
        if (TerminalStatuses.Contains(currentStatus))
        {
            throw new InvalidOperationException(
                $"Order cannot have its status changed. Current status '{currentStatus}' is a terminal state.");
        }

        var newStatus = request.Status?.Trim();
        if (string.IsNullOrEmpty(newStatus) || !ValidStatuses.Contains(newStatus))
        {
            throw new ArgumentException(
                $"Invalid status '{request.Status}'. Valid statuses are: {string.Join(", ", ValidStatuses)}.");
        }

        var fromStatus = order.OrderStatus ?? string.Empty;
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

        var historyEntry = new OrderStatusHistory
        {
            OrderId = order.OrderId,
            FromStatus = fromStatus,
            ToStatus = newStatus,
            Note = request.Note,
            ChangedBy = staffId,
            ChangedAt = DateTime.UtcNow
        };
        _context.OrderStatusHistories.Add(historyEntry);

        await _context.SaveChangesAsync();

        return MapToOrderOpsDetailDto(order);
    }

    private static OrderOpsDetailDto MapToOrderOpsDetailDto(Order order)
    {
        return new OrderOpsDetailDto
        {
            OrderId = order.OrderId,
            OrderCode = $"ORD-{order.OrderId:D6}",
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

    public async Task<PaginatedResultDto<OpsOrderListItemDto>> GetOrdersAsync(OpsOrderListRequestDto request)
    {
        return await BuildOrdersQueryAsync(request);
    }

    public async Task<PaginatedResultDto<OpsOrderListItemDto>> GetReadyMadeOrdersAsync(OpsOrderListRequestDto request)
    {
        request.OrderType = "Ready-made";
        return await BuildOrdersQueryAsync(request);
    }

    public async Task<PaginatedResultDto<OpsOrderListItemDto>> GetPrescriptionOrdersAsync(OpsOrderListRequestDto request)
    {
        request.OrderType = "Prescription";
        return await BuildOrdersQueryAsync(request);
    }

    public async Task<PaginatedResultDto<OpsOrderListItemDto>> GetPreOrderOrdersAsync(OpsOrderListRequestDto request)
    {
        request.OrderType = "Pre-order";
        return await BuildOrdersQueryAsync(request);
    }

    private async Task<PaginatedResultDto<OpsOrderListItemDto>> BuildOrdersQueryAsync(OpsOrderListRequestDto request)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(o => o.OrderStatus != null &&
                o.OrderStatus.ToLower() == request.Status.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(request.OrderType))
        {
            query = query.Where(o => o.OrderType != null &&
                o.OrderType.ToLower() == request.OrderType.ToLower());
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(o => o.OrderDate >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(o => o.OrderDate <= request.DateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(o =>
                (o.Customer != null && o.Customer.FullName != null && o.Customer.FullName.ToLower().Contains(search)) ||
                (o.Customer != null && o.Customer.Email != null && o.Customer.Email.ToLower().Contains(search)) ||
                (o.OrderId.ToString() == search));
        }

        var totalItems = await query.CountAsync();

        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling((double)totalItems / pageSize);

        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OpsOrderListItemDto
            {
                OrderId = o.OrderId,
                OrderCode = $"ORD-{o.OrderId:D6}",
                CustomerName = o.Customer != null ? o.Customer.FullName ?? string.Empty : string.Empty,
                CustomerEmail = o.Customer != null ? o.Customer.Email ?? string.Empty : string.Empty,
                OrderType = o.OrderType ?? string.Empty,
                OrderStatus = o.OrderStatus ?? string.Empty,
                PaymentStatus = o.PaymentStatus ?? string.Empty,
                TotalAmount = o.TotalAmount,
                ItemCount = o.OrderItems.Count,
                CreatedAt = o.OrderDate ?? DateTime.UtcNow
            })
            .ToListAsync();

        return new PaginatedResultDto<OpsOrderListItemDto>
        {
            Success = true,
            Data = items,
            Meta = new PaginationMeta
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            },
            Timestamp = DateTime.UtcNow
        };
    }
}
