using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.Product;
using VisionCare.BusinessLogicLayer.DTOs.ManagerProduct;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class ProductService : IProductService
{
    private readonly VisionCareContext _context;

    public ProductService(VisionCareContext context)
    {
        _context = context;
    }

    public async Task<(List<ProductResponseDto> Products, int TotalCount)> GetProductsAsync(GetProductsRequestDto request)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductVariants)
            .AsQueryable();

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(p => p.ProductName.ToLower().Contains(search));
        }

        if (request.IsPreOrder.HasValue)
        {
            query = query.Where(p => p.IsPreOrder == request.IsPreOrder.Value);
        }

        var totalCount = await query.CountAsync();

        query = (request.SortBy?.ToLower(), request.SortOrder?.ToLower()) switch
        {
            ("price", "asc") => query.OrderBy(p => p.BasePrice),
            ("price", "desc") => query.OrderByDescending(p => p.BasePrice),
            ("name", "asc") => query.OrderBy(p => p.ProductName),
            ("name", "desc") => query.OrderByDescending(p => p.ProductName),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 12 : request.PageSize;

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var productDtos = products.Select(p =>
        {
            var totalStock = p.ProductVariants.Sum(v => v.StockQuantity ?? 0);
            decimal minPrice;

            if (p.ProductVariants.Any())
            {
                minPrice = p.ProductVariants.Min(v => p.BasePrice + (v.AdditionalPrice ?? 0));
            }
            else
            {
                minPrice = p.BasePrice;
            }

            return new ProductResponseDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Brand = p.Brand,
                Description = p.Description,
                BasePrice = p.BasePrice,
                MinPrice = minPrice,
                TotalStock = totalStock,
                IsPreOrder = p.IsPreOrder,
                Image2D = p.Image2D,
                Model3D = p.Model3D,
                CreatedAt = p.CreatedAt,
                Category = p.Category != null
                    ? new CategoryDto { CategoryId = p.Category.CategoryId, CategoryName = p.Category.CategoryName }
                    : null
            };
        }).ToList();

        return (productDtos, totalCount);
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(int id)
    {
        var p = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (p == null)
        {
            return null;
        }

        return new ProductDetailDto
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Brand = p.Brand,
            Description = p.Description,
            BasePrice = p.BasePrice,
            IsPreOrder = p.IsPreOrder,
            Image2D = p.Image2D,
            Model3D = p.Model3D,
            CreatedAt = p.CreatedAt,
            Category = p.Category != null
                ? new CategoryDto { CategoryId = p.Category.CategoryId, CategoryName = p.Category.CategoryName }
                : null,
            ProductVariants = p.ProductVariants.Select(v => new ProductVariantDto
            {
                VariantId = v.VariantId,
                Color = v.Color,
                Size = v.Size,
                Sku = v.Sku,
                StockQuantity = v.StockQuantity,
                AdditionalPrice = v.AdditionalPrice ?? 0,
                EffectivePrice = p.BasePrice + (v.AdditionalPrice ?? 0)
            }).ToList()
        };
    }

    public async Task<ProductDetailDto> CreateManagerProductAsync(CreateManagerProductDto dto)
    {
        var product = new Product
        {
            ProductName = dto.ProductName,
            Brand = dto.Brand,
            Description = dto.Description,
            BasePrice = dto.BasePrice,
            CategoryId = dto.CategoryId,
            IsPreOrder = dto.IsPreOrder,
            Image2D = dto.Image2D,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Create default variant so it can be procured
        var defaultVariant = new ProductVariant
        {
            ProductId = product.ProductId,
            Color = "Mặc định",
            Size = "Standard",
            Sku = $"PRD-{product.ProductId}-DEF",
            StockQuantity = 0,
            AdditionalPrice = 0
        };
        _context.ProductVariants.Add(defaultVariant);
        await _context.SaveChangesAsync();

        return (await GetProductByIdAsync(product.ProductId))!;
    }

    public async Task<ProductDetailDto?> UpdateManagerProductAsync(int id, UpdateManagerProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return null;

        product.ProductName = dto.ProductName;
        product.Brand = dto.Brand;
        product.Description = dto.Description;
        product.BasePrice = dto.BasePrice;
        product.CategoryId = dto.CategoryId;
        product.IsPreOrder = dto.IsPreOrder;
        product.Image2D = dto.Image2D;

        await _context.SaveChangesAsync();
        return await GetProductByIdAsync(id);
    }

    public async Task<bool> DeleteManagerProductAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null) return false;

        // Note: In a real system, we'd check if variants are used in Orders/Receipts before hard deleting.
        // For simplicity, we remove variants then the product.
        _context.ProductVariants.RemoveRange(product.ProductVariants);
        _context.Products.Remove(product);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            throw new Exception("Không thể xóa sản phẩm này vì đã có dữ liệu liên kết (Đơn hàng/Nhập kho).");
        }
    }
}
