using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.OpsOrder;
using VisionCare.BusinessLogicLayer.DTOs.OpsPreOrder;
using VisionCare.BusinessLogicLayer.DTOs.Shipping;
using VisionCare.BusinessLogicLayer.Interfaces;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class OpsOrderService : IOpsOrderService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "Confirmed", "Processing", "Packed", "Shipped", "Delivered", "Completed", "Cancelled"
    };

    private static readonly HashSet<string> PackableStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "Confirmed", "Processing"
    };

    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cancelled", "Completed"
    };

    private static readonly HashSet<string> LensWorkAllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Confirmed", "Processing"
    };

    private static readonly HashSet<string> LensWorkTerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cancelled", "Delivered", "Completed", "Packed", "Shipped"
    };

    private readonly VisionCareContext _context;
    private readonly IShippingService _shippingService;

    public OpsOrderService(VisionCareContext context, IShippingService shippingService)
    {
        _context = context;
        _shippingService = shippingService;
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

        if (string.Equals(newStatus, "Delivered", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Customer must confirm receipt before the order can be completed.");
        }

        if (string.Equals(newStatus, "Completed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(currentStatus, "Delivered", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only customer-confirmed delivered orders can be completed.");
        }

        if (string.Equals(currentStatus, "Delivered", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(newStatus, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Customer-confirmed delivered orders can only be completed.");
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

        if (string.Equals(newStatus, "Shipped", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(newStatus, "Dispatched", StringComparison.OrdinalIgnoreCase))
        {
            await DecreaseStockForOrderAsync(order.OrderId, staffId, $"Status manual update to {newStatus}");
        }

        await _context.SaveChangesAsync();

        return MapToOrderOpsDetailDto(order);
    }

    public async Task<OrderOpsDetailDto> GetOrderDetailAsync(int id, bool isPreOrder = false)
    {
        if (isPreOrder)
        {
            var res = await _context.PreOrderReservations
                .Include(r => r.Customer)
                .Include(r => r.Variant)
                    .ThenInclude(v => v!.Product)
                .Include(r => r.Campaign)
                .FirstOrDefaultAsync(r => r.ReservationId == id);

            if (res == null)
            {
                throw new KeyNotFoundException($"Pre-order Reservation with ID {id} not found.");
            }

            return MapReservationToOrderOpsDetailDto(res);
        }

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v!.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Prescription)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order != null)
        {
            return MapToOrderOpsDetailDto(order);
        }

        // Fallback for backward compatibility if flag not provided correctly
        var reservation = await _context.PreOrderReservations
            .Include(r => r.Customer)
            .Include(r => r.Variant)
                .ThenInclude(v => v!.Product)
            .Include(r => r.Campaign)
            .FirstOrDefaultAsync(r => r.ReservationId == id);

        if (reservation == null)
        {
            throw new KeyNotFoundException($"Order or Reservation with ID {id} not found.");
        }

        return MapReservationToOrderOpsDetailDto(reservation);
    }

    private static OrderOpsDetailDto MapReservationToOrderOpsDetailDto(PreOrderReservation r)
    {
        return new OrderOpsDetailDto
        {
            OrderId = r.ReservationId,
            OrderCode = r.ReservationCode ?? $"RES-{r.ReservationId}",
            CustomerName = r.Customer?.FullName ?? string.Empty,
            CustomerEmail = r.Customer?.Email ?? string.Empty,
            OrderType = "Pre-order",
            OrderStatus = r.Status ?? string.Empty,
            PaymentStatus = r.PaidAt != null ? "Paid" : "Pending",
            TotalAmount = r.UnitPrice * r.ReservedQuantity,
            ShippingAddress = r.ShippingAddress ?? string.Empty,
            OrderDate = r.CreatedAt,
            StaffNote = string.Empty,
            Items = new List<OrderItemOpsDto>
            {
                new OrderItemOpsDto
                {
                    OrderItemId = r.ReservationId, // Mapping 1-1 for simplicity
                    VariantId = r.VariantId,
                    ProductName = r.Variant?.Product?.ProductName ?? "Unknown Product",
                    VariantInfo = FormatVariantInfo(r.Variant),
                    Quantity = r.ReservedQuantity,
                    UnitPrice = r.UnitPrice
                }
            }
        };
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
                UnitPrice = i.UnitPrice,
                PrescriptionId = i.PrescriptionId
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
        // Auto-fix stranded released reservations that were not converted
        var strandedReservations = await _context.PreOrderReservations
            .Where(r => r.Status != null && r.Status.ToLower() == "released" && !r.ConvertedOrderId.HasValue)
            .ToListAsync();
            
        if (strandedReservations.Any())
        {
            foreach(var r in strandedReservations)
            {
                var newOrder = new Order {
                    CustomerId = r.CustomerId,
                    OrderType = "Pre-order",
                    OrderStatus = "Processing",
                    PaymentStatus = "Paid",
                    TotalAmount = r.UnitPrice * r.ReservedQuantity,
                    PaidAmount = r.UnitPrice * r.ReservedQuantity,
                    OrderDate = DateTime.UtcNow,
                    StaffNote = $"Auto-converted (repair) from PreOrderReservation {r.ReservationCode}",
                    ShippingAddress = r.ShippingAddress
                };
                newOrder.OrderItems.Add(new OrderItem { 
                    VariantId = r.VariantId, 
                    Quantity = r.ReservedQuantity, 
                    UnitPrice = r.UnitPrice 
                });
                _context.Orders.Add(newOrder);
                await _context.SaveChangesAsync();
                r.ConvertedOrderId = newOrder.OrderId;
            }
            await _context.SaveChangesAsync();
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

        // Pre-orders are stored in PreOrderReservations, NOT in Orders.
        // Show reservations with status 'sent_to_ops' (waiting for stock) and other active statuses.
        var activeStatuses = new[]
        {
            "sent_to_ops",
            "stock_arrived",
            "customer_notified",
            "paid",
            "released"
        };

        var query = _context.PreOrderReservations
            .Include(r => r.Customer)
            .Include(r => r.Variant)
                .ThenInclude(v => v!.Product)
            .Include(r => r.Campaign)
            .Where(r => r.Status != null && activeStatuses.Contains(r.Status.ToLower()) && !r.ConvertedOrderId.HasValue)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(r => r.Status != null && r.Status.ToLower() == request.Status.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(r =>
                (r.Customer != null && r.Customer.FullName != null && r.Customer.FullName.ToLower().Contains(search)) ||
                r.ReservationCode.ToLower().Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling((double)totalItems / pageSize);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new OpsOrderListItemDto
            {
                OrderId = r.ReservationId,
                OrderCode = r.ReservationCode,
                CustomerName = r.Customer != null ? r.Customer.FullName ?? string.Empty : string.Empty,
                CustomerEmail = r.Customer != null ? r.Customer.Email ?? string.Empty : string.Empty,
                OrderType = "Pre-order",
                OrderStatus = r.Status,
                PaymentStatus = r.PaidAt != null ? "DepositPaid" : "Pending",
                TotalAmount = r.UnitPrice * r.ReservedQuantity,
                ItemCount = 1,
                CreatedAt = r.CreatedAt,
                // Extra info for Ops display
                ShippingAddress = r.ShippingAddress,
                IsPreOrder = true
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

    public async Task<LensWorkDetailDto> GetLensWorkAsync(int orderId)
    {
        var order = await LoadOrderForLensWorkAsync(orderId);

        if (!string.Equals(order.OrderType, "Prescription", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Lens work is only available for prescription orders. Order type is '{order.OrderType}'.");
        }

        return MapToLensWorkDetailDto(order);
    }

    public async Task<LensWorkDetailDto> AssignLensWorkAsync(int orderId, int staffId, AssignLensWorkRequestDto request)
    {
        var order = await LoadOrderForLensWorkAsync(orderId);

        if (!string.Equals(order.OrderType, "Prescription", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Lens work is only available for prescription orders. Order type is '{order.OrderType}'.");
        }

        var currentStatus = order.OrderStatus ?? string.Empty;
        if (!LensWorkAllowedStatuses.Contains(currentStatus))
        {
            throw new InvalidOperationException(
                $"Order cannot be assigned lens work. Current status is '{currentStatus}'. Only orders with status 'Confirmed' or 'Processing' can have lens work assigned.");
        }

        var lensMakerExists = await _context.Users.AnyAsync(u => u.UserId == request.AssignedLensMakerId);
        if (!lensMakerExists)
        {
            throw new KeyNotFoundException($"Lens maker with ID {request.AssignedLensMakerId} not found.");
        }

        var prescriptionItems = order.OrderItems.Where(oi => oi.PrescriptionId.HasValue).ToList();
        if (prescriptionItems.Count == 0)
        {
            throw new InvalidOperationException("No prescription items found in this order.");
        }

        foreach (var item in prescriptionItems)
        {
            item.AssignedLensMakerId = request.AssignedLensMakerId;
        }

        await _context.SaveChangesAsync();

        return MapToLensWorkDetailDto(order);
    }

    public async Task<LensWorkDetailDto> CompleteLensWorkAsync(int orderId, int staffId, CompleteLensWorkRequestDto request)
    {
        var order = await LoadOrderForLensWorkAsync(orderId);

        if (!string.Equals(order.OrderType, "Prescription", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Lens work is only available for prescription orders. Order type is '{order.OrderType}'.");
        }

        var currentStatus = order.OrderStatus ?? string.Empty;
        if (LensWorkTerminalStatuses.Contains(currentStatus))
        {
            throw new InvalidOperationException(
                $"Order cannot complete lens work. Current status is '{currentStatus}'.");
        }

        var prescriptionItems = order.OrderItems.Where(oi => oi.PrescriptionId.HasValue).ToList();
        if (prescriptionItems.Count == 0)
        {
            throw new InvalidOperationException("No prescription items found in this order.");
        }

        foreach (var item in prescriptionItems)
        {
            item.LensCutCompletedAt = DateTime.UtcNow;
            item.LensCutNote = request.Note;
        }

        var allDone = prescriptionItems.All(pi => pi.LensCutCompletedAt.HasValue);
        if (allDone)
        {
            var fromStatus = order.OrderStatus ?? string.Empty;
            order.OrderStatus = "LensCutComplete";

            var historyEntry = new OrderStatusHistory
            {
                OrderId = order.OrderId,
                FromStatus = fromStatus,
                ToStatus = "LensCutComplete",
                Note = $"Lens cut completed by staff ID {staffId}.",
                ChangedBy = staffId,
                ChangedAt = DateTime.UtcNow
            };
            _context.OrderStatusHistories.Add(historyEntry);
        }

        await _context.SaveChangesAsync();

        return MapToLensWorkDetailDto(order);
    }

    private async Task<Order> LoadOrderForLensWorkAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Prescription)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.AssignedLensMaker)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }

        return order;
    }

    private static LensWorkDetailDto MapToLensWorkDetailDto(Order order)
    {
        return new LensWorkDetailDto
        {
            OrderId = order.OrderId,
            OrderCode = $"ORD-{order.OrderId:D6}",
            OrderStatus = order.OrderStatus ?? string.Empty,
            Items = order.OrderItems.Select(i => new LensWorkItemDto
            {
                OrderItemId = i.OrderItemId,
                VariantId = i.VariantId,
                ProductName = i.Variant?.Product?.ProductName ?? string.Empty,
                VariantInfo = FormatVariantInfo(i.Variant),
                Quantity = i.Quantity,
                PrescriptionId = i.PrescriptionId,
                OdSphere = i.Prescription?.OdSphere,
                OdCylinder = i.Prescription?.OdCylinder,
                OdAxis = i.Prescription?.OdAxis,
                OsSphere = i.Prescription?.OsSphere,
                OsCylinder = i.Prescription?.OsCylinder,
                OsAxis = i.Prescription?.OsAxis,
                Pd = i.Prescription?.Pd,
                LensNote = i.Prescription?.Note,
                AssignedLensMakerId = i.AssignedLensMakerId,
                AssignedLensMakerName = i.AssignedLensMaker?.FullName,
                LensCutCompletedAt = i.LensCutCompletedAt,
                LensCutNote = i.LensCutNote,
                IsLensCutComplete = i.LensCutCompletedAt.HasValue
            }).ToList()
        };
    }

    // ─── Pre-Order Receive / Fulfill ───────────────────────────────────────────

    public async Task<List<PreOrderReceiveListDto>> GetPreOrderReceiveListAsync(string? status, int? campaignId)
    {
        var filterStatus = string.IsNullOrWhiteSpace(status) ? "active" : status.Trim();

        var query = _context.PreOrderCampaigns
            .Include(c => c.CampaignProducts)
            .ThenInclude(cp => cp.Variant)
            .ThenInclude(v => v!.Product)
            .Include(c => c.Reservations)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filterStatus))
        {
            query = query.Where(c => c.Status.ToLower() == filterStatus.ToLower());
        }

        if (campaignId.HasValue)
        {
            query = query.Where(c => c.CampaignId == campaignId.Value);
        }

        var campaigns = await query.ToListAsync();
        var result = new List<PreOrderReceiveListDto>();

        foreach (var campaign in campaigns)
        {
            var paidReservations = campaign.Reservations
                .Where(r => r.Status != null && r.Status.ToLower() == "paid")
                .ToList();

            var totalReserved = paidReservations.Sum(r => r.ReservedQuantity);
            var totalReceived = campaign.CampaignProducts.Sum(cp => cp.ReceivedQuantity);
            var pendingQuantity = totalReserved - totalReceived;
            var isReadyToFulfill = pendingQuantity <= 0 && totalReserved > 0;

            var items = campaign.CampaignProducts.Select(cp =>
            {
                var itemReserved = paidReservations
                    .Where(r => r.VariantId == cp.VariantId)
                    .Sum(r => r.ReservedQuantity);
                return new PreOrderReceiveItemDto
                {
                    VariantId = cp.VariantId ?? 0,
                    Sku = cp.Variant?.Sku ?? string.Empty,
                    ProductName = cp.Variant?.Product?.ProductName ?? string.Empty,
                    ReservedQuantity = itemReserved,
                    ReceivedQuantity = cp.ReceivedQuantity,
                    PendingQuantity = itemReserved - cp.ReceivedQuantity,
                    CampaignPrice = cp.CampaignPrice
                };
            }).ToList();

            result.Add(new PreOrderReceiveListDto
            {
                CampaignId = campaign.CampaignId,
                CampaignCode = campaign.CampaignCode,
                CampaignName = campaign.CampaignName,
                Status = campaign.Status,
                ReleaseDate = campaign.ReleaseDate,
                TotalPaidReservations = paidReservations.Count,
                TotalReservedQuantity = totalReserved,
                TotalReceivedQuantity = totalReceived,
                PendingQuantity = pendingQuantity,
                IsReadyToFulfill = isReadyToFulfill,
                Items = items
            });
        }

        return result;
    }

    public async Task<PreOrderReceiveResultDto> ReceivePreOrderAsync(int campaignId, int staffId, ReceivePreOrderRequestDto request)
    {
        var campaign = await _context.PreOrderCampaigns
            .Include(c => c.CampaignProducts)
            .ThenInclude(cp => cp.Variant)
            .Include(c => c.Reservations)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null)
        {
            throw new KeyNotFoundException($"Pre-order campaign with ID {campaignId} not found.");
        }

        var warehouse = await _context.Warehouses.FindAsync(request.WarehouseId);
        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {request.WarehouseId} not found.");
        }

        var paidReservations = campaign.Reservations
            .Where(r => r.Status != null && r.Status.ToLower() == "paid")
            .ToList();

        if (paidReservations.Count == 0)
        {
            throw new InvalidOperationException("No paid reservations found for this campaign.");
        }

        var variantIds = paidReservations.Select(r => r.VariantId).Distinct().ToList();
        var campaignProducts = campaign.CampaignProducts
            .Where(cp => cp.VariantId.HasValue && variantIds.Contains(cp.VariantId.Value))
            .ToList();

        if (campaignProducts.Count == 0)
        {
            throw new InvalidOperationException("No campaign products found for the reserved variants.");
        }

        var totalReserved = paidReservations.Sum(r => r.ReservedQuantity);

        // Update ReceivedQuantity on each campaign product
        foreach (var cp in campaignProducts)
        {
            cp.ReceivedQuantity += request.ReceivedQuantity;
        }

        // Update or create Inventory for the first variant+warehouse
        var firstVariantId = campaignProducts.First().VariantId!.Value;
        var inventory = await _context.Inventories
            .FirstOrDefaultAsync(i => i.VariantId == firstVariantId && i.WarehouseId == request.WarehouseId);

        if (inventory == null)
        {
            inventory = new Inventory
            {
                VariantId = firstVariantId,
                WarehouseId = request.WarehouseId,
                QuantityOnHand = request.ReceivedQuantity,
                QuantityReserved = 0,
                QuantityDefective = 0,
                QuantityTransit = 0,
                BatchNumber = request.BatchNumber,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Inventories.Add(inventory);
        }
        else
        {
            var previousOnHand = inventory.QuantityOnHand;
            inventory.QuantityOnHand += request.ReceivedQuantity;
            inventory.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.BatchNumber))
            {
                inventory.BatchNumber = request.BatchNumber;
            }
        }

        // Record StockMovement
        var stockMovement = new StockMovement
        {
            VariantId = firstVariantId,
            WarehouseId = request.WarehouseId,
            MovementType = "PREORDER_RECEIVE",
            QuantityBefore = inventory.QuantityOnHand - request.ReceivedQuantity,
            QuantityChange = request.ReceivedQuantity,
            QuantityAfter = inventory.QuantityOnHand,
            ReferenceType = "preorder",
            ReferenceId = campaign.CampaignId,
            PerformedBy = staffId,
            PerformedAt = DateTime.UtcNow,
            StaffNote = request.Note
        };
        _context.StockMovements.Add(stockMovement);

        // Mark ALL paid reservations as fulfilled
        foreach (var reservation in paidReservations)
        {
            reservation.Status = "fulfilled";
            reservation.FulfilledAt = DateTime.UtcNow;
        }

        // Re-evaluate campaign status
        var newTotalReceived = campaignProducts.Sum(cp => cp.ReceivedQuantity);
        if (newTotalReceived >= totalReserved)
        {
            campaign.Status = "fulfilled";
            campaign.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var remaining = Math.Max(0, totalReserved - newTotalReceived);

        return new PreOrderReceiveResultDto
        {
            CampaignId = campaign.CampaignId,
            CampaignCode = campaign.CampaignCode,
            ReceivedQuantity = request.ReceivedQuantity,
            TotalReceivedNow = newTotalReceived,
            TotalReceived = newTotalReceived,
            RemainingQuantity = remaining,
            Status = campaign.Status,
            Message = remaining > 0
                ? $"Partial receive: {newTotalReceived}/{totalReserved} units received."
                : $"Full receive complete: {newTotalReceived} units received."
        };
    }

    public async Task<ShippingOrderDto?> FulfillPreOrderAsync(int campaignId, int staffId, OpsFulfillPreOrderRequestDto request)
    {
        var campaign = await _context.PreOrderCampaigns
            .Include(c => c.Reservations)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null)
        {
            throw new KeyNotFoundException($"Pre-order campaign with ID {campaignId} not found.");
        }

        var fulfilledReservations = campaign.Reservations
            .Where(r => r.Status != null && r.Status.ToLower() == "fulfilled" && r.ConvertedOrderId.HasValue)
            .ToList();

        if (fulfilledReservations.Count == 0)
        {
            throw new InvalidOperationException(
                "No fulfilled reservations with converted orders found for this campaign.");
        }

        var reservation = fulfilledReservations.First();
        var orderId = reservation.ConvertedOrderId!.Value;

        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Converted order with ID {orderId} not found.");
        }

        var createShippingRequest = new CreateShippingOrderRequestDto
        {
            ShippingMethodId = request.ShippingMethodId,
            WeightKg = request.WeightKg
        };

        var shippingOrder = await _shippingService.CreateShippingOrderAsync(orderId, staffId, createShippingRequest);

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
            OrderId = orderId,
            FromStatus = order.OrderStatus ?? string.Empty,
            ToStatus = "Dispatched",
            Note = $"Pre-order campaign '{campaign.CampaignName}' fulfilled by staff ID {staffId}.",
            ChangedBy = staffId,
            ChangedAt = DateTime.UtcNow
        };
        _context.OrderStatusHistories.Add(historyEntry);

        await _context.SaveChangesAsync();
        await DecreaseStockForOrderAsync(orderId, staffId, $"Pre-order campaign '{campaign.CampaignName}' fulfilled.");

        return shippingOrder;
    }

    private async Task DecreaseStockForOrderAsync(int orderId, int staffId, string note)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null) return;

        // Prevent double reduction
        var alreadyReduced = await _context.StockMovements
            .AnyAsync(m => m.ReferenceType == "order" && m.ReferenceId == orderId && m.MovementType == "SHIPMENT_OUT");
        if (alreadyReduced) return;

        var primaryWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.IsPrimary && w.IsActive)
                               ?? await _context.Warehouses.FirstOrDefaultAsync(w => w.IsActive);
        if (primaryWarehouse == null) return;

        foreach (var item in order.OrderItems)
        {
            // VariantId is an int, so it always has a value.
            if (item.VariantId == 0) continue;

            // 1. Update ProductVariant total
            var variant = await _context.ProductVariants.FindAsync(item.VariantId);
            if (variant != null)
            {
                variant.StockQuantity -= item.Quantity;
            }

            // 2. Update Inventory
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.VariantId == item.VariantId && i.WarehouseId == primaryWarehouse.WarehouseId);

            if (inventory != null)
            {
                var qtyBefore = inventory.QuantityOnHand;
                inventory.QuantityOnHand -= item.Quantity;
                inventory.UpdatedAt = DateTime.UtcNow;

                // 3. Record StockMovement
                _context.StockMovements.Add(new StockMovement
                {
                    VariantId = item.VariantId,
                    WarehouseId = primaryWarehouse.WarehouseId,
                    MovementType = "SHIPMENT_OUT",
                    QuantityBefore = qtyBefore,
                    QuantityChange = -item.Quantity,
                    QuantityAfter = inventory.QuantityOnHand,
                    ReferenceType = "order",
                    ReferenceId = orderId,
                    PerformedBy = staffId,
                    PerformedAt = DateTime.UtcNow,
                    StaffNote = note
                });
            }
        }
    }

    public async Task<bool> MarkPreOrderStockArrivedAsync(int reservationId, int opsStaffId)
    {
        var reservation = await _context.PreOrderReservations
            .Include(r => r.Variant)
            .Include(r => r.Campaign)
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

        if (reservation == null) return false;

        // Current status must be 'sent_to_ops'
        if (reservation.Status != VisionCare.BusinessLogicLayer.Constants.PreOrderStatuses.SentToOps)
        {
            throw new InvalidOperationException("Chỉ có thể báo có hàng cho các đơn đang chờ xử lý tại bộ phận Ops.");
        }

        reservation.Status = VisionCare.BusinessLogicLayer.Constants.PreOrderStatuses.StockArrived;
        await _context.SaveChangesAsync();

        // NOTIFY Sales Staff
        // We notify staff who can handle pre-orders (roleId 3)
        var salesStaff = await _context.Users.Where(u => u.RoleId == 3).ToListAsync();
        foreach (var staff in salesStaff)
        {
            // Injecting NotificationService would cause a circular dependency if not careful
            // For now, we'll manually create the notification record or I'll inject it if safe.
            // Assuming I'll update the constructor to include INotificationService.
            var notification = new Notification
            {
                UserId = staff.UserId,
                Title = "Hàng Pre-order đã về kho",
                Message = $"Đơn {reservation.ReservationCode} ({reservation.Variant?.Sku}) đã có hàng. Vui lòng gọi báo khách thanh toán lần 2.",
                Type = "PreOrder",
                Link = $"/staff/pre-orders?reservationId={reservationId}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<OpsOrderListItemDto?> GetPreOrderReservationDetailAsync(int reservationId)
    {
        var r = await _context.PreOrderReservations
            .Include(r => r.Customer)
            .Include(r => r.Variant)
                .ThenInclude(v => v!.Product)
            .Include(r => r.Campaign)
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

        if (r == null) return null;

        return new OpsOrderListItemDto
        {
            OrderId = r.ReservationId,
            OrderCode = r.ReservationCode,
            CustomerName = r.Customer?.FullName ?? string.Empty,
            CustomerEmail = r.Customer?.Email ?? string.Empty,
            OrderType = "Pre-order",
            OrderStatus = r.Status,
            PaymentStatus = r.PaidAt != null ? "DepositPaid" : "Pending",
            TotalAmount = r.UnitPrice * r.ReservedQuantity,
            ItemCount = 1,
            CreatedAt = r.CreatedAt,
            ShippingAddress = r.ShippingAddress,
            IsPreOrder = true
        };
    }
}
