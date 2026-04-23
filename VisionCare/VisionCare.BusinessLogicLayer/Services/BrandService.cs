using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs;
using VisionCare.BusinessLogicLayer.Interfaces;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class BrandService : IBrandService
{
    private readonly VisionCareContext _context;

    public BrandService(VisionCareContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BrandDto>> GetApprovedBrandsAsync()
    {
        return await _context.Brands
            .Where(b => b.Status == "Approved")
            .Select(b => MapToDto(b))
            .ToListAsync();
    }

    public async Task<IEnumerable<BrandDto>> GetPendingRequestsAsync()
    {
        return await _context.Brands
            .Where(b => b.Status == "Pending" || b.Status == "PendingDeletion")
            .OrderByDescending(b => b.RequestDate)
            .Select(b => MapToDto(b))
            .ToListAsync();
    }

    public async Task<BrandDto> RequestAddBrandAsync(CreateBrandRequestDto request, int userId, string role)
    {
        var brand = new Brand
        {
            BrandName = request.BrandName,
            Description = request.Description,
            EvidenceUrl = request.EvidenceUrl,
            Status = role.ToLower() == "manager" ? "Approved" : "Pending",
            RequestType = "Add",
            RequestedBy = userId,
            RequestDate = DateTime.UtcNow,
            ApprovalDate = role.ToLower() == "manager" ? DateTime.UtcNow : null,
            ApprovedBy = role.ToLower() == "manager" ? userId : null
        };

        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();

        return MapToDto(brand);
    }

    public async Task<BrandDto> RequestDeleteBrandAsync(int brandId, DeleteBrandRequestDto request, int userId)
    {
        var brand = await _context.Brands.FindAsync(brandId);
        if (brand == null) return null!;

        brand.Status = "PendingDeletion";
        brand.RequestType = "Delete";
        brand.Reason = request.Reason;
        brand.EvidenceUrl = request.EvidenceUrl ?? brand.EvidenceUrl;
        brand.RequestedBy = userId;
        brand.RequestDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(brand);
    }

    public async Task<bool> ProcessBrandRequestAsync(int brandId, BrandActionDto action, int managerId)
    {
        var brand = await _context.Brands.FindAsync(brandId);
        if (brand == null) return false;

        if (action.Status == "Approved")
        {
            if (brand.RequestType == "Delete")
            {
                // Soft delete or actual delete? 
                // For now, let's just mark as Deleted or remove.
                // User said "Update: Only those approved appear".
                // So if we delete, we can just remove from DB or set status to Deleted.
                brand.Status = "Deleted";
            }
            else
            {
                brand.Status = "Approved";
            }
        }
        else if (action.Status == "Rejected")
        {
            // If was Pending -> Rejected
            // If was PendingDeletion -> back to Approved
            brand.Status = brand.RequestType == "Delete" ? "Approved" : "Rejected";
        }

        brand.ApprovedBy = managerId;
        brand.ApprovalDate = DateTime.UtcNow;
        brand.ManagerNote = action.ManagerNote;

        await _context.SaveChangesAsync();
        return true;
    }

    private static BrandDto MapToDto(Brand b)
    {
        return new BrandDto
        {
            BrandId = b.BrandId,
            BrandName = b.BrandName,
            Description = b.Description,
            EvidenceUrl = b.EvidenceUrl,
            Status = b.Status,
            RequestType = b.RequestType,
            Reason = b.Reason,
            RequestedBy = b.RequestedBy,
            RequesterName = b.Requester?.FullName,
            RequestDate = b.RequestDate,
            ApprovedBy = b.ApprovedBy,
            ApproverName = b.Approver?.FullName,
            ApprovalDate = b.ApprovalDate,
            ManagerNote = b.ManagerNote
        };
    }
}
