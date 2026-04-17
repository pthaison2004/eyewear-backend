using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.SalesComplaint;
using VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class SalesComplaintService : ISalesComplaintService
{
    private readonly VisionCareContext _context;
    private readonly ILogger<SalesComplaintService> _logger;

    private static readonly HashSet<string> ValidPriorities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "low", "normal", "high", "urgent"
    };

    public SalesComplaintService(VisionCareContext context, ILogger<SalesComplaintService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ComplaintListDto>> GetComplaintsAsync(string? statusFilter, string? priorityFilter)
    {
        var query = _context.Complaints
            .Include(c => c.Customer)
            .Include(c => c.Order)
            .Include(c => c.AssignedToUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(c => c.ComplaintStatus == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(priorityFilter))
        {
            query = query.Where(c => c.Priority == priorityFilter);
        }

        var complaints = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return complaints.Select(c => new ComplaintListDto
        {
            ComplaintId = c.ComplaintId,
            OrderId = c.OrderId,
            OrderCode = $"ORD-{c.OrderId:D5}",
            CustomerName = c.Customer?.FullName ?? string.Empty,
            ComplaintType = c.ComplaintType,
            Subject = c.Subject,
            ComplaintStatus = c.ComplaintStatus,
            Priority = c.Priority,
            AssignedToName = c.AssignedToUser?.FullName,
            CreatedAt = c.CreatedAt,
            ProcessedAt = c.ProcessedAt,
            ResolvedAt = c.ResolvedAt
        }).ToList();
    }

    public async Task<ComplaintDetailDto> CreateComplaintAsync(int orderId, int staffId, CreateComplaintRequestDto request)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Order not found with ID: {orderId}");
        }

        if (!order.CustomerId.HasValue)
        {
            throw new InvalidOperationException("Order does not have a customer assigned.");
        }

        if (!ValidPriorities.Contains(request.Priority))
        {
            throw new InvalidOperationException($"Invalid priority value: {request.Priority}. Allowed values: low, normal, high, urgent.");
        }

        var complaint = new Complaint
        {
            OrderId = orderId,
            CustomerId = order.CustomerId.Value,
            ComplaintType = request.ComplaintType,
            Subject = request.Subject,
            Description = request.Description,
            ComplaintStatus = "open",
            Priority = request.Priority.ToLower(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Complaints.Add(complaint);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Complaint {ComplaintId} created for order {OrderId} by staff {StaffId}", complaint.ComplaintId, orderId, staffId);

        return await GetComplaintDetailAsync(complaint.ComplaintId);
    }

    public async Task<ComplaintResponseDto> ProcessComplaintAsync(int complaintId, int staffId, ProcessComplaintRequestDto request)
    {
        var complaint = await _context.Complaints.FindAsync(complaintId);
        if (complaint == null)
        {
            throw new KeyNotFoundException($"Complaint not found with ID: {complaintId}");
        }

        if (complaint.ComplaintStatus != "open")
        {
            throw new InvalidOperationException($"Cannot process complaint with status: {complaint.ComplaintStatus}");
        }

        complaint.ComplaintStatus = "processing";
        complaint.ProcessedAt = DateTime.UtcNow;
        complaint.ProcessedBy = staffId;

        if (!string.IsNullOrWhiteSpace(request.ProcessedNote))
        {
            complaint.ProcessedNote = request.ProcessedNote;
        }

        if (request.AssignedTo.HasValue)
        {
            complaint.AssignedTo = request.AssignedTo;
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            complaint.Priority = request.Priority;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Complaint {ComplaintId} processed by staff {StaffId}", complaintId, staffId);

        string? assignedToName = null;
        if (complaint.AssignedTo.HasValue)
        {
            var assignedUser = await _context.Users.FindAsync(complaint.AssignedTo.Value);
            assignedToName = assignedUser?.FullName;
        }

        return new ComplaintResponseDto
        {
            ComplaintId = complaint.ComplaintId,
            ComplaintStatus = complaint.ComplaintStatus,
            AssignedToName = assignedToName,
            UpdatedAt = DateTime.UtcNow,
            Message = "Complaint is now being processed."
        };
    }

    public async Task<ComplaintResponseDto> ResolveComplaintAsync(int complaintId, int staffId, ResolveComplaintRequestDto request)
    {
        var complaint = await _context.Complaints.FindAsync(complaintId);
        if (complaint == null)
        {
            throw new KeyNotFoundException($"Complaint not found with ID: {complaintId}");
        }

        if (complaint.ComplaintStatus != "processing" && complaint.ComplaintStatus != "open")
        {
            throw new InvalidOperationException($"Cannot resolve complaint with status: {complaint.ComplaintStatus}");
        }

        complaint.ComplaintStatus = "resolved";
        complaint.ResolvedAt = DateTime.UtcNow;
        complaint.ResolvedBy = staffId;
        complaint.Resolution = request.Resolution;

        if (!string.IsNullOrWhiteSpace(request.ProcessedNote) && string.IsNullOrWhiteSpace(complaint.ProcessedNote))
        {
            complaint.ProcessedNote = request.ProcessedNote;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Complaint {ComplaintId} resolved by staff {StaffId}", complaintId, staffId);

        string? assignedToName = null;
        if (complaint.AssignedTo.HasValue)
        {
            var assignedUser = await _context.Users.FindAsync(complaint.AssignedTo.Value);
            assignedToName = assignedUser?.FullName;
        }

        return new ComplaintResponseDto
        {
            ComplaintId = complaint.ComplaintId,
            ComplaintStatus = complaint.ComplaintStatus,
            AssignedToName = assignedToName,
            UpdatedAt = DateTime.UtcNow,
            Message = "Complaint has been resolved."
        };
    }

    private async Task<ComplaintDetailDto> GetComplaintDetailAsync(int complaintId)
    {
        var complaint = await _context.Complaints
            .Include(c => c.Order)
            .Include(c => c.Customer)
            .Include(c => c.AssignedToUser)
            .Include(c => c.ProcessedByUser)
            .Include(c => c.ResolvedByUser)
            .FirstOrDefaultAsync(c => c.ComplaintId == complaintId);

        if (complaint == null)
        {
            throw new KeyNotFoundException($"Complaint not found with ID: {complaintId}");
        }

        return new ComplaintDetailDto
        {
            ComplaintId = complaint.ComplaintId,
            OrderId = complaint.OrderId,
            OrderCode = $"ORD-{complaint.OrderId:D5}",
            CustomerName = complaint.Customer?.FullName ?? string.Empty,
            CustomerEmail = complaint.Customer?.Email ?? string.Empty,
            CustomerPhone = complaint.Customer?.PhoneNumber ?? string.Empty,
            ComplaintType = complaint.ComplaintType,
            Subject = complaint.Subject,
            Description = complaint.Description,
            ComplaintStatus = complaint.ComplaintStatus,
            Priority = complaint.Priority,
            AssignedToName = complaint.AssignedToUser?.FullName,
            ProcessedNote = complaint.ProcessedNote,
            Resolution = complaint.Resolution,
            CustomerSatisfaction = complaint.CustomerSatisfaction,
            CreatedAt = complaint.CreatedAt,
            ProcessedAt = complaint.ProcessedAt,
            ProcessedByName = complaint.ProcessedByUser?.FullName,
            ResolvedAt = complaint.ResolvedAt,
            ResolvedByName = complaint.ResolvedByUser?.FullName,
            UpdatedAt = complaint.UpdatedAt
        };
    }
}
