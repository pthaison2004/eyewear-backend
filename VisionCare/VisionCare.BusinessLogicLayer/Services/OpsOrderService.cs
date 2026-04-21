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
        "Pending", "Confirmed", "Processing", "Packed", "Shipped", "Delivered", "Cancelled", "Completed"
    };

    private static readonly HashSet<string> PackableStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "Confirmed", "Processing"
    };

    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cancelled", "Delivered", "Completed"
    };

    private static readonly HashSet<string> LensWorkAllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Confirmed", "Processing"
    };

    private static readonly HashSet<string> LensWorkTerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cancelled", "Delivered", "Packed", "Shipped", "Completed"
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

    public async Task<OrderOpsDetailDto> GetOrderByIdAsync(int orderId)
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

        return MapToOrderOpsDetailDto(order);
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
                HasPrescription = o.OrderItems.Any(i => i.PrescriptionId != null),
                IsPreOrder = string.Equals(o.OrderType, "PreOrder", StringComparison.OrdinalIgnoreCase),
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

    public async Task<ShippingOrderDto?> FulfillPreOrderAsync(int campaignId, int staffId, FulfillPreOrderRequestDto request)
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

        return shippingOrder;
    }
}
