using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.Supplier;
using VisionCare.BusinessLogicLayer.Interfaces;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class SupplierService : ISupplierService
{
    private readonly VisionCareContext _context;

    public SupplierService(VisionCareContext context)
    {
        _context = context;
    }

    public async Task<List<SupplierDto>> GetAllAsync()
    {
        var suppliers = await _context.Suppliers
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return suppliers.Select(MapToDto).ToList();
    }

    public async Task<SupplierDto?> GetByIdAsync(int id)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierId == id);

        return supplier == null ? null : MapToDto(supplier);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequestDto request)
    {
        var maxSeq = await _context.Suppliers
            .Select(s => s.SupplierCode)
            .ToListAsync();

        int nextSeq = 1;
        foreach (var code in maxSeq)
        {
            if (!string.IsNullOrEmpty(code) && code.StartsWith("SUP-"))
            {
                var numPart = code.Substring(4);
                if (int.TryParse(numPart, out int num))
                {
                    if (num >= nextSeq)
                        nextSeq = num + 1;
                }
            }
        }

        var supplier = new Supplier
        {
            SupplierCode = $"SUP-{nextSeq:D3}",
            SupplierName = request.SupplierName,
            ContactName = request.ContactName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Address = request.Address,
            TaxCode = request.TaxCode,
            Notes = request.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return MapToDto(supplier);
    }

    public async Task<SupplierDto?> UpdateAsync(int id, UpdateSupplierRequestDto request)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierId == id);

        if (supplier == null)
            return null;

        if (request.SupplierName != null)
            supplier.SupplierName = request.SupplierName;
        if (request.ContactName != null)
            supplier.ContactName = request.ContactName;
        if (request.PhoneNumber != null)
            supplier.PhoneNumber = request.PhoneNumber;
        if (request.Email != null)
            supplier.Email = request.Email;
        if (request.Address != null)
            supplier.Address = request.Address;
        if (request.TaxCode != null)
            supplier.TaxCode = request.TaxCode;
        if (request.IsActive.HasValue)
            supplier.IsActive = request.IsActive.Value;
        if (request.Notes != null)
            supplier.Notes = request.Notes;

        supplier.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(supplier);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierId == id);

        if (supplier == null)
            return false;

        supplier.IsActive = false;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<SupplierDto>> GetActiveAsync()
    {
        var suppliers = await _context.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.SupplierName)
            .ToListAsync();

        return suppliers.Select(MapToDto).ToList();
    }

    private static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto
        {
            SupplierId = supplier.SupplierId,
            SupplierCode = supplier.SupplierCode,
            SupplierName = supplier.SupplierName,
            ContactName = supplier.ContactName,
            PhoneNumber = supplier.PhoneNumber,
            Email = supplier.Email,
            Address = supplier.Address,
            TaxCode = supplier.TaxCode,
            IsActive = supplier.IsActive,
            Notes = supplier.Notes,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        };
    }
}
