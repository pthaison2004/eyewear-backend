using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.Shipping;
using VisionCare.BusinessLogicLayer.Interfaces;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class ShippingService : IShippingService
{
    private static readonly HashSet<string> ShippableStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Packed", "Processing"
    };

    private readonly VisionCareContext _context;

    public ShippingService(VisionCareContext context)
    {
        _context = context;
    }

    public async Task<List<ShippingMethodDto>> GetAvailableShippingMethodsAsync()
    {
        return await _context.ShippingMethods
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder)
            .Select(m => new ShippingMethodDto
            {
                ShippingMethodId = m.ShippingMethodId,
                MethodCode = m.MethodCode,
                MethodName = m.MethodName,
                Provider = m.Provider,
                BaseFee = m.BaseFee,
                FeePerKg = m.FeePerKg,
                FreeShippingThreshold = m.FreeShippingThreshold,
                EstimatedDaysMin = m.EstimatedDaysMin,
                EstimatedDaysMax = m.EstimatedDaysMax,
                CodAvailable = m.CodAvailable,
                MaxCodAmount = m.MaxCodAmount
            })
            .ToListAsync();
    }

    public async Task<ShippingOrderDto> CreateShippingOrderAsync(int orderId, int staffId, CreateShippingOrderRequestDto request)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.ShippingOrders)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }

        if (!ShippableStatuses.Contains(order.OrderStatus ?? string.Empty))
        {
            throw new InvalidOperationException(
                $"Order cannot be shipped. Current status is '{order.OrderStatus}'. Only orders with status 'Packed' or 'Processing' can be dispatched.");
        }

        if (order.ShippingOrders.Any())
        {
            throw new InvalidOperationException(
                $"Order {orderId} already has a shipping order.");
        }

        var shippingMethod = await _context.ShippingMethods
            .FirstOrDefaultAsync(m => m.ShippingMethodId == request.ShippingMethodId && m.IsActive);

        if (shippingMethod == null)
        {
            throw new KeyNotFoundException($"Shipping method with ID {request.ShippingMethodId} not found or is inactive.");
        }

        var weightKg = request.WeightKg ?? 0.5m;
        var declaredValue = request.DeclaredValue ?? order.TotalAmount;

        var shippingFee = shippingMethod.BaseFee + (weightKg * shippingMethod.FeePerKg);

        var codFee = 0m;
        if (shippingMethod.CodAvailable)
        {
            codFee = Math.Max(0.01m * declaredValue, 5000m);
        }

        var insuranceFee = declaredValue > 5000000m ? declaredValue * 0.001m : 0m;

        var totalShippingCost = shippingFee + codFee + insuranceFee;

        var shippingAddress = order.ShippingAddress ?? string.Empty;
        var addressParts = shippingAddress.Split(',', StringSplitOptions.TrimEntries);

        var recipientName = order.Customer?.FullName ?? string.Empty;
        var phoneNumber = order.Customer?.PhoneNumber ?? string.Empty;
        string provinceCode = string.Empty;
        string districtCode = string.Empty;
        string wardCode = string.Empty;
        string streetAddress = shippingAddress;

        if (addressParts.Length >= 3)
        {
            wardCode = addressParts[^3];
            districtCode = addressParts[^2];
            provinceCode = addressParts[^1];
            streetAddress = string.Join(", ", addressParts.Take(addressParts.Length - 3));
        }
        else if (addressParts.Length == 2)
        {
            districtCode = addressParts[0];
            provinceCode = addressParts[1];
            streetAddress = string.Empty;
        }
        else if (addressParts.Length == 1)
        {
            provinceCode = addressParts[0];
            streetAddress = string.Empty;
        }

        var todayPrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var lastSeq = await _context.ShippingOrders
            .Where(s => s.ShippingOrderCode.StartsWith($"SHP-{todayPrefix}"))
            .Select(s => s.ShippingOrderCode)
            .ToListAsync();

        var nextSeq = 1;
        if (lastSeq.Count > 0)
        {
            var maxSeq = lastSeq
                .Select(code =>
                {
                    var parts = code.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out var seq))
                        return seq;
                    return 0;
                })
                .DefaultIfEmpty(0)
                .Max();
            nextSeq = maxSeq + 1;
        }

        var shippingOrderCode = $"SHP-{todayPrefix}-{nextSeq:D4}";

        var carrierOrderNo = $"MOCK-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";

        var shippingOrder = new ShippingOrder
        {
            ShippingOrderCode = shippingOrderCode,
            OrderId = orderId,
            ShippingMethodId = shippingMethod.ShippingMethodId,
            CarrierOrderNo = carrierOrderNo,
            CarrierStatus = "Created",
            RecipientName = recipientName,
            PhoneNumber = phoneNumber,
            ProvinceCode = provinceCode,
            DistrictCode = districtCode,
            WardCode = wardCode,
            StreetAddress = streetAddress,
            ShippingFee = shippingFee,
            CodFee = codFee,
            InsuranceFee = insuranceFee,
            TotalShippingCost = totalShippingCost,
            ShippingStatusId = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.ShippingOrders.Add(shippingOrder);

        var fromStatus = order.OrderStatus ?? string.Empty;
        order.OrderStatus = "Dispatched";
        order.TrackingNumber = carrierOrderNo;

        var historyEntry = new OrderStatusHistory
        {
            OrderId = orderId,
            FromStatus = fromStatus,
            ToStatus = "Dispatched",
            Note = $"Shipping order created. Carrier: {shippingMethod.MethodName}. Fee: {totalShippingCost:N0} VND. Staff: {staffId}.",
            ChangedBy = staffId,
            ChangedAt = DateTime.UtcNow
        };
        _context.OrderStatusHistories.Add(historyEntry);

        await _context.SaveChangesAsync();

        return MapToDto(shippingOrder, order, shippingMethod);
    }

    public async Task<ShippingOrderDto?> MarkOrderAsShippedAsync(int orderId, int staffId)
    {
        var shippingOrder = await _context.ShippingOrders
            .Include(s => s.Order)
            .Include(s => s.ShippingMethod)
            .Where(s => s.OrderId == orderId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (shippingOrder == null)
        {
            return null;
        }

        shippingOrder.ShippedAt = DateTime.UtcNow;
        shippingOrder.ShippingStatusId = 2;
        shippingOrder.CarrierStatus = "PickedUp";

        if (shippingOrder.Order != null)
        {
            var fromStatus = shippingOrder.Order.OrderStatus ?? string.Empty;
            shippingOrder.Order.OrderStatus = "Shipped";

            var historyEntry = new OrderStatusHistory
            {
                OrderId = orderId,
                FromStatus = fromStatus,
                ToStatus = "Shipped",
                Note = $"Order shipped via {shippingOrder.ShippingMethod?.MethodName}. Staff: {staffId}.",
                ChangedBy = staffId,
                ChangedAt = DateTime.UtcNow
            };
            _context.OrderStatusHistories.Add(historyEntry);
        }

        await _context.SaveChangesAsync();

        return MapToDto(shippingOrder, shippingOrder.Order, shippingOrder.ShippingMethod);
    }

    public async Task<List<ShippingStatusDto>> GetShippingStatusesAsync()
    {
        return await _context.ShippingStatuses
            .OrderBy(s => s.StatusOrder)
            .Select(s => new ShippingStatusDto
            {
                ShippingStatusId = s.ShippingStatusId,
                StatusCode = s.StatusCode,
                StatusName = s.StatusName,
                StatusOrder = s.StatusOrder
            })
            .ToListAsync();
    }

    public async Task<ShippingOrderDto?> UpdateShippingStatusAsync(int shippingOrderId, int staffId, UpdateShippingStatusRequestDto request)
    {
        var shippingOrder = await _context.ShippingOrders
            .Include(s => s.Order)
            .Include(s => s.ShippingMethod)
            .Include(s => s.StatusHistories)
            .FirstOrDefaultAsync(s => s.ShippingOrderId == shippingOrderId);

        if (shippingOrder == null)
        {
            return null;
        }

        var targetStatus = await _context.ShippingStatuses
            .FirstOrDefaultAsync(s => s.ShippingStatusId == request.StatusId);

        if (targetStatus == null)
        {
            throw new KeyNotFoundException($"Shipping status with ID {request.StatusId} not found.");
        }

        var currentStatus = await _context.ShippingStatuses
            .FirstOrDefaultAsync(s => s.ShippingStatusId == shippingOrder.ShippingStatusId);

        if (currentStatus != null && currentStatus.StatusOrder >= 5)
        {
            throw new InvalidOperationException(
                $"Cannot update shipping status. Current status '{currentStatus.StatusName}' is a terminal state and cannot be changed.");
        }

        var fromStatusId = shippingOrder.ShippingStatusId;
        shippingOrder.ShippingStatusId = request.StatusId;
        shippingOrder.CarrierStatus = targetStatus.StatusName;

        if (targetStatus.StatusOrder == 5)
        {
            shippingOrder.DeliveredAt = DateTime.UtcNow;
        }

        var history = new ShippingStatusHistory
        {
            ShippingOrderId = shippingOrderId,
            FromStatusId = fromStatusId,
            ToStatusId = request.StatusId,
            CarrierStatusText = request.Note,
            Location = request.Location,
            UpdatedBy = staffId,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ShippingStatusHistories.Add(history);

        await _context.SaveChangesAsync();

        return MapToDto(shippingOrder, shippingOrder.Order, shippingOrder.ShippingMethod);
    }

    public async Task<List<ShippingStatusHistoryDto>> GetShippingHistoryAsync(int shippingOrderId)
    {
        return await _context.ShippingStatusHistories
            .Include(h => h.FromStatus)
            .Include(h => h.ToStatus)
            .Where(h => h.ShippingOrderId == shippingOrderId)
            .OrderByDescending(h => h.UpdatedAt)
            .Select(h => new ShippingStatusHistoryDto
            {
                HistoryId = h.HistoryId,
                ShippingOrderId = h.ShippingOrderId,
                FromStatus = h.FromStatus != null ? h.FromStatus.StatusName : null,
                ToStatus = h.ToStatus!.StatusName,
                CarrierStatusText = h.CarrierStatusText,
                Location = h.Location,
                UpdatedAt = h.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<ShippingTrackingDto?> TrackShippingAsync(string trackingNo)
    {
        var shippingOrder = await _context.ShippingOrders
            .Include(s => s.StatusHistories)
                .ThenInclude(h => h.FromStatus)
            .Include(s => s.StatusHistories)
                .ThenInclude(h => h.ToStatus)
            .FirstOrDefaultAsync(s =>
                s.CarrierTrackingNo == trackingNo ||
                s.CarrierOrderNo == trackingNo ||
                s.ShippingOrderCode == trackingNo);

        if (shippingOrder == null)
        {
            return null;
        }

        var currentStatus = await _context.ShippingStatuses
            .FirstOrDefaultAsync(s => s.ShippingStatusId == shippingOrder.ShippingStatusId);

        var fullAddress = string.Join(", ",
            new[] { shippingOrder.StreetAddress, shippingOrder.WardCode, shippingOrder.DistrictCode, shippingOrder.ProvinceCode }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        var history = shippingOrder.StatusHistories
            .OrderByDescending(h => h.UpdatedAt)
            .Select(h => new ShippingStatusHistoryDto
            {
                HistoryId = h.HistoryId,
                ShippingOrderId = h.ShippingOrderId,
                FromStatus = h.FromStatus != null ? h.FromStatus.StatusName : null,
                ToStatus = h.ToStatus!.StatusName,
                CarrierStatusText = h.CarrierStatusText,
                Location = h.Location,
                UpdatedAt = h.UpdatedAt
            })
            .ToList();

        return new ShippingTrackingDto
        {
            ShippingOrderId = shippingOrder.ShippingOrderId,
            ShippingOrderCode = shippingOrder.ShippingOrderCode,
            CarrierTrackingNo = shippingOrder.CarrierTrackingNo,
            CarrierOrderNo = shippingOrder.CarrierOrderNo,
            CurrentStatus = currentStatus?.StatusName ?? string.Empty,
            RecipientName = shippingOrder.RecipientName,
            FullAddress = fullAddress,
            History = history
        };
    }

    public async Task<ShippingOrderDto?> MarkAsDeliveredAsync(int orderId, int staffId)
    {
        var shippingOrder = await _context.ShippingOrders
            .Include(s => s.Order)
            .Include(s => s.ShippingMethod)
            .Include(s => s.StatusHistories)
            .Where(s => s.OrderId == orderId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (shippingOrder == null)
        {
            throw new InvalidOperationException("No shipping order found for this order.");
        }

        if (shippingOrder.ShippingStatusId >= 5)
        {
            throw new InvalidOperationException("Already delivered or terminal.");
        }

        shippingOrder.ShippingStatusId = 5;
        shippingOrder.DeliveredAt = DateTime.UtcNow;
        shippingOrder.CarrierStatus = "Delivered";

        var order = shippingOrder.Order;
        if (order == null)
        {
            throw new InvalidOperationException("No related order found for this shipping order.");
        }

        var terminalStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Cancelled",
            "Delivered"
        };

        if (terminalStatuses.Contains(order.OrderStatus ?? string.Empty))
        {
            throw new InvalidOperationException(
                $"Order status '{order.OrderStatus}' is already terminal and cannot be updated.");
        }

        var fromStatus = order.OrderStatus ?? string.Empty;
        order.OrderStatus = "Delivered";

        var orderHistory = new OrderStatusHistory
        {
            OrderId = orderId,
            FromStatus = fromStatus,
            ToStatus = "Delivered",
            Note = "Delivery confirmed by staff",
            ChangedBy = staffId,
            ChangedAt = DateTime.UtcNow
        };
        _context.OrderStatusHistories.Add(orderHistory);

        var shippingHistory = new ShippingStatusHistory
        {
            ShippingOrderId = shippingOrder.ShippingOrderId,
            FromStatusId = shippingOrder.ShippingStatusId,
            ToStatusId = 5,
            CarrierStatusText = "Delivery confirmed",
            UpdatedBy = staffId,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ShippingStatusHistories.Add(shippingHistory);

        await _context.SaveChangesAsync();

        return MapToDto(shippingOrder, order, shippingOrder.ShippingMethod);
    }

    private static ShippingOrderDto MapToDto(ShippingOrder so, Order? order, ShippingMethod? method)
    {
        var fullAddress = string.Join(", ",
            new[] { so.StreetAddress, so.WardCode, so.DistrictCode, so.ProvinceCode }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        return new ShippingOrderDto
        {
            ShippingOrderId = so.ShippingOrderId,
            ShippingOrderCode = so.ShippingOrderCode,
            OrderId = so.OrderId,
            OrderCode = order != null ? $"ORD-{order.OrderId:D6}" : string.Empty,
            ShippingMethodId = so.ShippingMethodId,
            ShippingMethodName = method?.MethodName ?? string.Empty,
            CarrierTrackingNo = so.CarrierTrackingNo,
            CarrierOrderNo = so.CarrierOrderNo,
            CarrierStatus = so.CarrierStatus,
            RecipientName = so.RecipientName,
            PhoneNumber = so.PhoneNumber,
            FullAddress = fullAddress,
            ShippingFee = so.ShippingFee,
            CodFee = so.CodFee,
            InsuranceFee = so.InsuranceFee,
            TotalShippingCost = so.TotalShippingCost,
            ShippingStatusId = so.ShippingStatusId,
            CreatedAt = so.CreatedAt,
            ShippedAt = so.ShippedAt
        };
    }
}
