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
