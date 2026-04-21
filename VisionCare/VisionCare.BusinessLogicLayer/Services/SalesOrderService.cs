using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.SalesOrder;
using VisionCare.BusinessLogicLayer.DTOs.SalesPayment;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class SalesOrderService : ISalesOrderService
{
    private readonly VisionCareContext _context;

    public SalesOrderService(VisionCareContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResultDto<SalesOrderListItemDto>> GetOrdersAsync(SalesOrderListRequestDto request)
    {
        return await BuildOrdersQueryAsync(request);
    }

    public async Task<PaginatedResultDto<SalesOrderListItemDto>> GetPendingOrdersAsync(SalesOrderListRequestDto request)
    {
        request.Status = "Pending";
        return await BuildOrdersQueryAsync(request);
    }

    public async Task<SalesOrderDetailDto> GetOrderDetailAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Prescription)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID {orderId}.");
        }

        return MapToDetailDto(order);
    }

    public async Task<SalesOrderDetailDto> ConfirmOrderAsync(int orderId, int staffId, ConfirmOrderRequestDto request)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Prescription)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID {orderId}.");
        }

        if (!string.Equals(order.OrderStatus, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể xác nhận đơn hàng. Trạng thái hiện tại là '{order.OrderStatus}', chỉ đơn ở trạng thái 'Pending' mới được phép xác nhận.");
        }

        order.OrderStatus = "Confirmed";

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            AppendStaffNote(order, staffId, request.Note);
        }

        AddStatusHistory(order, "Pending", "Confirmed", request.Note, staffId);

        await _context.SaveChangesAsync();

        return MapToDetailDto(order);
    }

    public async Task<SalesOrderDetailDto> RejectOrderAsync(int orderId, int staffId, RejectOrderRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Lý do từ chối không được để trống.");
        }

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID {orderId}.");
        }

        if (!string.Equals(order.OrderStatus, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể từ chối đơn hàng. Trạng thái hiện tại là '{order.OrderStatus}', chỉ đơn ở trạng thái 'Pending' mới được phép từ chối.");
        }

        order.OrderStatus = "Cancelled";

        AppendStaffNote(order, staffId, $"REJECTED: {request.Reason}");

        AddStatusHistory(order, "Pending", "Cancelled", request.Reason, staffId);

        await _context.SaveChangesAsync();

        return MapToDetailDto(order);
    }

    public async Task<SalesOrderDetailDto> AssignOrderAsync(int orderId, int staffId, AssignOrderRequestDto request)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID {orderId}.");
        }

        if (!string.Equals(order.OrderStatus, "Confirmed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể giao đơn cho Operations. Trạng thái hiện tại là '{order.OrderStatus}', chỉ đơn ở trạng thái 'Confirmed' mới được phép giao.");
        }

        order.OrderStatus = "Processing";

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            AppendStaffNote(order, staffId, request.Note);
        }

        AddStatusHistory(order, "Confirmed", "Processing", request.Note, staffId);

        await _context.SaveChangesAsync();

        return MapToDetailDto(order);
    }

    public async Task<SalesOrderDetailDto> AddStaffNoteAsync(int orderId, int staffId, StaffNoteRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Note))
        {
            throw new ArgumentException("Ghi chú không được để trống.");
        }

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID {orderId}.");
        }

        AppendStaffNote(order, staffId, request.Note);

        await _context.SaveChangesAsync();

        return MapToDetailDto(order);
    }

    public async Task<SalesOrderDetailDto> MarkOrderPaidAsync(int orderId, int staffId, MarkPaidRequestDto request)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                .ThenInclude(v => v!.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Prescription)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID {orderId}.");

        if (string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đơn hàng đã được thanh toán.");

        if (!string.Equals(order.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(order.PaymentStatus, "Unpaid", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Không thể cập nhật thanh toán. Trạng thái hiện tại là '{order.PaymentStatus}'.");

        order.PaymentStatus = "Paid";

        var paymentInfo = $"PAYMENT [{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Method={request.PaymentMethod}";
        if (request.AmountPaid.HasValue)
            paymentInfo += $", Amount={request.AmountPaid.Value:N0}VND";
        if (!string.IsNullOrWhiteSpace(request.TransactionRef))
            paymentInfo += $", Ref={request.TransactionRef}";
        paymentInfo += $", Staff={staffId}";

        AppendStaffNote(order, staffId, paymentInfo);
        if (!string.IsNullOrWhiteSpace(request.Note))
            AppendStaffNote(order, staffId, $"Note: {request.Note}");

        await _context.SaveChangesAsync();
        return MapToDetailDto(order);
    }

    private async Task<PaginatedResultDto<SalesOrderListItemDto>> BuildOrdersQueryAsync(SalesOrderListRequestDto request)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Prescription)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(o => o.OrderStatus != null &&
                o.OrderStatus.ToLower() == request.Status.ToLower());
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
            .Select(o => new SalesOrderListItemDto
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
                HasPrescription = o.OrderItems.Any(i => i.PrescriptionId != null),
                IsPrescriptionVerified = o.OrderItems.Where(i => i.PrescriptionId != null).All(i => i.Prescription != null && i.Prescription.IsVerified),
                OrderDate = o.OrderDate ?? DateTime.UtcNow
            })
            .ToListAsync();

        return new PaginatedResultDto<SalesOrderListItemDto>
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

    private void AppendStaffNote(Order order, int staffId, string note)
    {
        var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] [{staffId}] {note}";
        order.StaffNote = string.IsNullOrEmpty(order.StaffNote)
            ? entry
            : $"{order.StaffNote}\n{entry}";
    }

    private void AddStatusHistory(Order order, string fromStatus, string toStatus, string? note, int staffId)
    {
        var historyEntry = new OrderStatusHistory
        {
            OrderId = order.OrderId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Note = note,
            ChangedBy = staffId,
            ChangedAt = DateTime.UtcNow
        };
        _context.OrderStatusHistories.Add(historyEntry);
    }

    private static SalesOrderDetailDto MapToDetailDto(Order order)
    {
        return new SalesOrderDetailDto
        {
            OrderId = order.OrderId,
            OrderCode = $"ORD-{order.OrderId:D6}",
            CustomerName = order.Customer?.FullName ?? string.Empty,
            CustomerEmail = order.Customer?.Email ?? string.Empty,
            CustomerPhone = order.Customer?.PhoneNumber,
            OrderType = order.OrderType ?? string.Empty,
            OrderStatus = order.OrderStatus ?? string.Empty,
            PaymentStatus = order.PaymentStatus ?? string.Empty,
            TotalAmount = order.TotalAmount,
            ShippingAddress = order.ShippingAddress,
            OrderDate = order.OrderDate,
            PackedAt = order.PackedAt,
            StaffNote = order.StaffNote,
            Items = order.OrderItems.Select(i => new SalesOrderItemDto
            {
                OrderItemId = i.OrderItemId,
                VariantId = i.VariantId,
                ProductName = i.Variant?.Product?.ProductName ?? string.Empty,
                VariantInfo = FormatVariantInfo(i.Variant),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Quantity * i.UnitPrice,
                PrescriptionId = i.PrescriptionId,
                OdSphere = i.Prescription?.OdSphere,
                OdCylinder = i.Prescription?.OdCylinder,
                OdAxis = i.Prescription?.OdAxis,
                OsSphere = i.Prescription?.OsSphere,
                OsCylinder = i.Prescription?.OsCylinder,
                OsAxis = i.Prescription?.OsAxis,
                Pd = i.Prescription?.Pd,
                IsPrescriptionVerified = i.Prescription?.IsVerified ?? false,
                IsPrescriptionRejected = i.Prescription?.IsRejected ?? false,
                IsPrescriptionExpired = i.Prescription?.CreatedAt.HasValue == true &&
                    i.Prescription.CreatedAt.Value.AddMonths(24) < DateTime.UtcNow
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