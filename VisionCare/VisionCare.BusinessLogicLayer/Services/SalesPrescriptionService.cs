using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.SalesPrescription;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class SalesPrescriptionService : ISalesPrescriptionService
{
    private readonly VisionCareContext _context;
    private readonly ILogger<SalesPrescriptionService> _logger;

    public SalesPrescriptionService(VisionCareContext context, ILogger<SalesPrescriptionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SalesPrescriptionListDto>> GetPrescriptionOrdersAsync(string? search, int? orderStatusFilter)
    {
        var query = _context.Prescriptions
            .Include(p => p.Customer)
            .Include(p => p.OrderItems)
                .ThenInclude(oi => oi.Order)
            .Where(p => p.OrderItems.Any(oi => oi.Order != null && oi.Order.OrderType == "Prescription"));

        if (orderStatusFilter.HasValue)
        {
            query = query.Where(p => p.OrderItems.Any(oi => oi.Order != null && oi.Order.OrderStatus == GetOrderStatusString(orderStatusFilter.Value)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p =>
                (p.Customer != null && (p.Customer.FullName.ToLower().Contains(searchLower) || (p.Customer.Email != null && p.Customer.Email.ToLower().Contains(searchLower)))));
        }

        var prescriptions = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return prescriptions.Select(p =>
        {
            var linkedOrder = p.OrderItems
                .Where(oi => oi.Order != null)
                .Select(oi => oi.Order!)
                .FirstOrDefault();

            var ageInMonths = p.CreatedAt.HasValue
                ? (int)((DateTime.UtcNow - p.CreatedAt.Value).TotalDays / 30.44)
                : (int?)null;

            return new SalesPrescriptionListDto
            {
                PrescriptionId = p.PrescriptionId,
                CustomerId = p.CustomerId ?? 0,
                CustomerName = p.Customer?.FullName ?? string.Empty,
                CustomerEmail = p.Customer?.Email ?? string.Empty,
                OdSphere = p.OdSphere,
                OdCylinder = p.OdCylinder,
                OsSphere = p.OsSphere,
                OsCylinder = p.OsCylinder,
                Pd = p.Pd,
                CreatedAt = p.CreatedAt ?? DateTime.MinValue,
                IsVerified = p.IsVerified,
                IsRejected = p.IsRejected,
                LinkedOrderId = linkedOrder?.OrderId,
                LinkedOrderCode = linkedOrder != null ? $"ORD-{linkedOrder.OrderId:D5}" : null,
                AgeInMonths = ageInMonths
            };
        }).ToList();
    }

    public async Task<PrescriptionReviewDto> GetPrescriptionReviewAsync(int prescriptionId)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Customer)
            .Include(p => p.OrderItems)
                .ThenInclude(oi => oi.Order)
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

        if (prescription == null)
        {
            throw new KeyNotFoundException($"Prescription not found with ID: {prescriptionId}");
        }

        var ageInMonths = prescription.CreatedAt.HasValue
            ? (int)((DateTime.UtcNow - prescription.CreatedAt.Value).TotalDays / 30.44)
            : (int?)null;

        var rules = await _context.PrescriptionValidationRules
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .ToListAsync();

        var validationResults = new List<ValidationResultDto>();

        foreach (var rule in rules)
        {
            var result = EvaluateRule(rule, prescription, ageInMonths);
            validationResults.Add(result);
        }

        string? verifiedByName = null;
        string? rejectedByName = null;

        if (prescription.VerifiedBy.HasValue)
        {
            var verifier = await _context.Users.FindAsync(prescription.VerifiedBy.Value);
            verifiedByName = verifier?.FullName;
        }

        if (prescription.RejectedBy.HasValue)
        {
            var rejector = await _context.Users.FindAsync(prescription.RejectedBy.Value);
            rejectedByName = rejector?.FullName;
        }

        var expiryRule = rules.FirstOrDefault(r => r.RuleType == "expiry_months");
        var warningRule = rules.FirstOrDefault(r => r.RuleType == "age_warning");

        var isExpired = expiryRule != null && ageInMonths.HasValue && ageInMonths.Value > expiryRule.MaxValue;
        var isOlderThan12Months = warningRule != null && ageInMonths.HasValue && ageInMonths.Value > warningRule.MaxValue;

        return new PrescriptionReviewDto
        {
            PrescriptionId = prescription.PrescriptionId,
            CustomerId = prescription.CustomerId ?? 0,
            CustomerName = prescription.Customer?.FullName ?? string.Empty,
            CustomerEmail = prescription.Customer?.Email ?? string.Empty,
            CustomerPhone = prescription.Customer?.PhoneNumber ?? string.Empty,
            OdSphere = prescription.OdSphere,
            OdCylinder = prescription.OdCylinder,
            OdAxis = prescription.OdAxis,
            OsSphere = prescription.OsSphere,
            OsCylinder = prescription.OsCylinder,
            OsAxis = prescription.OsAxis,
            Pd = prescription.Pd,
            Note = prescription.Note,
            CreatedAt = prescription.CreatedAt ?? DateTime.MinValue,
            IsVerified = prescription.IsVerified,
            IsRejected = prescription.IsRejected,
            VerifiedAt = prescription.VerifiedAt,
            VerifiedByName = verifiedByName,
            RejectedAt = prescription.RejectedAt,
            RejectedByName = rejectedByName,
            RejectionReason = prescription.RejectionReason,
            LinkedOrders = prescription.OrderItems
                .Where(oi => oi.Order != null)
                .Select(oi => new LinkedOrderDto
                {
                    OrderId = oi.Order!.OrderId,
                    OrderCode = $"ORD-{oi.Order.OrderId:D5}",
                    OrderType = oi.Order.OrderType ?? string.Empty,
                    OrderStatus = oi.Order.OrderStatus ?? string.Empty,
                    TotalAmount = oi.Order.TotalAmount,
                    OrderDate = oi.Order.OrderDate ?? DateTime.MinValue
                }).ToList(),
            ValidationResults = validationResults,
            AgeInMonths = ageInMonths,
            IsExpired = isExpired,
            IsOlderThan12Months = isOlderThan12Months
        };
    }

    public async Task<PrescriptionReviewDto> VerifyPrescriptionAsync(int prescriptionId, int staffId, VerifyPrescriptionRequestDto request)
    {
        var prescription = await _context.Prescriptions.FindAsync(prescriptionId);

        if (prescription == null)
        {
            throw new KeyNotFoundException($"Prescription not found with ID: {prescriptionId}");
        }

        if (request.IsVerified)
        {
            prescription.IsVerified = true;
            prescription.VerifiedAt = DateTime.UtcNow;
            prescription.VerifiedBy = staffId;
            prescription.IsRejected = false;
            prescription.RejectedAt = null;
            prescription.RejectedBy = null;
            prescription.RejectionReason = null;

            _logger.LogInformation("Prescription {PrescriptionId} verified by staff {StaffId}", prescriptionId, staffId);
        }
        else
        {
            prescription.IsRejected = true;
            prescription.RejectedAt = DateTime.UtcNow;
            prescription.RejectedBy = staffId;
            prescription.RejectionReason = request.Notes;
            prescription.IsVerified = false;
            prescription.VerifiedAt = null;
            prescription.VerifiedBy = null;

            _logger.LogInformation("Prescription {PrescriptionId} rejected by staff {StaffId}: {Reason}", prescriptionId, staffId, request.Notes);
        }

        await _context.SaveChangesAsync();

        return await GetPrescriptionReviewAsync(prescriptionId);
    }

    public async Task<AdjustPrescriptionResponseDto> AdjustPrescriptionAsync(int prescriptionId, int staffId, AdjustPrescriptionRequestDto request)
    {
        var prescription = await _context.Prescriptions.FindAsync(prescriptionId);

        if (prescription == null)
        {
            throw new KeyNotFoundException($"Prescription not found with ID: {prescriptionId}");
        }

        var staff = await _context.Users.FindAsync(staffId);
        var staffName = staff?.FullName ?? "Unknown";

        var noteEntry = $"[Sales Staff: {staffName}] [{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {request.ContactReason}";
        if (!string.IsNullOrWhiteSpace(request.SuggestedCorrection))
        {
            noteEntry += $" \u2192 {request.SuggestedCorrection}";
        }

        if (string.IsNullOrWhiteSpace(prescription.Note))
        {
            prescription.Note = noteEntry;
        }
        else
        {
            prescription.Note += Environment.NewLine + noteEntry;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Prescription {PrescriptionId} adjusted by staff {StaffId}: {Reason}", prescriptionId, staffId, request.ContactReason);

        return new AdjustPrescriptionResponseDto
        {
            PrescriptionId = prescriptionId,
            ContactReason = request.ContactReason,
            SuggestedCorrection = request.SuggestedCorrection,
            StaffNote = prescription.Note,
            StaffId = staffId,
            StaffName = staffName,
            ContactedAt = DateTime.UtcNow
        };
    }

    private static ValidationResultDto EvaluateRule(PrescriptionValidationRule rule, Prescription prescription, int? ageInMonths)
    {
        return rule.RuleType switch
        {
            "sphere_max" => EvaluateSphereMax(rule, prescription),
            "cylinder_max" => EvaluateCylinderMax(rule, prescription),
            "pd_range" => EvaluatePdRange(rule, prescription),
            "age_warning" => EvaluateAgeWarning(rule, ageInMonths),
            "expiry_months" => EvaluateExpiry(rule, ageInMonths),
            "min_age" => EvaluateMinAge(rule, ageInMonths),
            _ => new ValidationResultDto { RuleName = rule.RuleName, RuleType = rule.RuleType, IsPassed = true, Message = "Unknown rule type" }
        };
    }

    private static ValidationResultDto EvaluateSphereMax(PrescriptionValidationRule rule, Prescription prescription)
    {
        var maxVal = rule.MaxValue ?? -20;
        var odPassed = prescription.OdSphere.HasValue && Math.Abs(prescription.OdSphere.Value) <= Math.Abs(maxVal);
        var osPassed = prescription.OsSphere.HasValue && Math.Abs(prescription.OsSphere.Value) <= Math.Abs(maxVal);
        var isPassed = odPassed && osPassed;

        return new ValidationResultDto
        {
            RuleName = rule.RuleName,
            RuleType = rule.RuleType,
            IsPassed = isPassed,
            Message = isPassed ? $"Sphere values within limit ({maxVal})" : $"Sphere exceeds maximum allowed value of {maxVal}"
        };
    }

    private static ValidationResultDto EvaluateCylinderMax(PrescriptionValidationRule rule, Prescription prescription)
    {
        var maxVal = rule.MaxValue ?? -6;
        var odPassed = !prescription.OdCylinder.HasValue || Math.Abs(prescription.OdCylinder.Value) <= Math.Abs(maxVal);
        var osPassed = !prescription.OsCylinder.HasValue || Math.Abs(prescription.OsCylinder.Value) <= Math.Abs(maxVal);
        var isPassed = odPassed && osPassed;

        return new ValidationResultDto
        {
            RuleName = rule.RuleName,
            RuleType = rule.RuleType,
            IsPassed = isPassed,
            Message = isPassed ? $"Cylinder values within limit ({maxVal})" : $"Cylinder exceeds maximum allowed value of {maxVal}"
        };
    }

    private static ValidationResultDto EvaluatePdRange(PrescriptionValidationRule rule, Prescription prescription)
    {
        var minVal = rule.MinValue ?? 50;
        var maxVal = rule.MaxValue ?? 80;
        var isPassed = prescription.Pd.HasValue && prescription.Pd.Value >= minVal && prescription.Pd.Value <= maxVal;

        return new ValidationResultDto
        {
            RuleName = rule.RuleName,
            RuleType = rule.RuleType,
            IsPassed = isPassed,
            Message = isPassed ? $"PD within range ({minVal}-{maxVal})" : $"PD {prescription.Pd} is outside valid range ({minVal}-{maxVal})"
        };
    }

    private static ValidationResultDto EvaluateAgeWarning(PrescriptionValidationRule rule, int? ageInMonths)
    {
        var maxVal = rule.MaxValue ?? 12;
        var isPassed = !ageInMonths.HasValue || ageInMonths.Value <= maxVal;

        return new ValidationResultDto
        {
            RuleName = rule.RuleName,
            RuleType = rule.RuleType,
            IsPassed = isPassed,
            Message = isPassed ? $"Prescription is within {maxVal} months" : $"Prescription is older than {maxVal} months — review recommended"
        };
    }

    private static ValidationResultDto EvaluateExpiry(PrescriptionValidationRule rule, int? ageInMonths)
    {
        var maxVal = rule.MaxValue ?? 24;
        var isPassed = !ageInMonths.HasValue || ageInMonths.Value <= maxVal;

        return new ValidationResultDto
        {
            RuleName = rule.RuleName,
            RuleType = rule.RuleType,
            IsPassed = isPassed,
            Message = isPassed ? $"Prescription is valid (within {maxVal} months)" : $"Prescription has expired — older than {maxVal} months"
        };
    }

    private static ValidationResultDto EvaluateMinAge(PrescriptionValidationRule rule, int? ageInMonths)
    {
        var minVal = rule.MinValue ?? 5;
        var isPassed = !ageInMonths.HasValue || ageInMonths.Value >= minVal;

        return new ValidationResultDto
        {
            RuleName = rule.RuleName,
            RuleType = rule.RuleType,
            IsPassed = isPassed,
            Message = isPassed ? $"Patient age sufficient" : $"Patient may be younger than minimum recommended age of {minVal} months"
        };
    }

    private static string GetOrderStatusString(int statusCode)
    {
        return statusCode switch
        {
            0 => "Pending",
            1 => "Processing",
            2 => "Shipped",
            3 => "Delivered",
            4 => "Cancelled",
            _ => "Pending"
        };
    }
}
