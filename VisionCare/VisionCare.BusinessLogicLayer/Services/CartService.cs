using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.Cart;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class CartService : ICartService
{
    private readonly VisionCareContext _context;

    public CartService(VisionCareContext context)
    {
        _context = context;
    }

    public async Task<CartResponseDto> GetCartAsync(int customerId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Variant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (cart == null)
        {
            return new CartResponseDto
            {
                CartId = 0,
                CustomerId = customerId,
                Items = new(),
                TotalAmount = 0
            };
        }

        return await MapToCartResponseDtoAsync(cart);
    }

    public async Task<CartResponseDto> AddItemAsync(int customerId, AddCartItemRequestDto request)
    {
        var cart = await GetOrCreateCartAsync(customerId);
        var variantId = await ResolveVariantIdAsync(request);

        var existingItem = cart.CartItems.FirstOrDefault(ci =>
            ci.VariantId == variantId &&
            ci.PrescriptionId == request.PrescriptionId);

        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            var variantExists = await _context.ProductVariants.AnyAsync(v => v.VariantId == variantId);
            if (!variantExists)
            {
                throw new InvalidOperationException($"Product variant with ID {variantId} does not exist.");
            }

            var newItem = new CartItem
            {
                CartId = cart.CartId,
                VariantId = variantId,
                PrescriptionId = request.PrescriptionId,
                Quantity = request.Quantity
            };
            _context.CartItems.Add(newItem);
        }

        await _context.SaveChangesAsync();

        return await GetCartAsync(customerId);
    }

    private async Task<int> ResolveVariantIdAsync(AddCartItemRequestDto request)
    {
        if (request.VariantId > 0)
        {
            return request.VariantId;
        }

        if (!request.ProductId.HasValue || request.ProductId.Value <= 0)
        {
            throw new InvalidOperationException("Vui lòng chọn biến thể sản phẩm hợp lệ.");
        }

        var product = await _context.Products
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.ProductId == request.ProductId.Value);

        if (product == null)
        {
            throw new InvalidOperationException($"Product with ID {request.ProductId.Value} does not exist.");
        }

        var variant = product.ProductVariants
            .OrderBy(v => v.VariantId)
            .FirstOrDefault();

        if (variant != null)
        {
            return variant.VariantId;
        }

        variant = new ProductVariant
        {
            ProductId = product.ProductId,
            Color = "Default",
            Size = "Standard",
            Sku = $"PO-{product.ProductId}",
            StockQuantity = 0,
            AdditionalPrice = 0
        };

        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();

        return variant.VariantId;
    }

    public async Task<CartResponseDto> AddComboAsync(int customerId, AddCartComboRequestDto request)
    {
        if (request.FrameVariantId <= 0)
        {
            throw new InvalidOperationException("Vui lòng chọn gọng kính hợp lệ.");
        }

        if (request.LensVariantId <= 0)
        {
            throw new InvalidOperationException("Vui lòng chọn tròng kính hợp lệ.");
        }

        var variantIds = new[] { request.FrameVariantId, request.LensVariantId };
        var existingVariantIds = await _context.ProductVariants
            .Where(v => variantIds.Contains(v.VariantId))
            .Select(v => v.VariantId)
            .ToListAsync();

        if (!existingVariantIds.Contains(request.FrameVariantId))
        {
            throw new InvalidOperationException($"Frame variant with ID {request.FrameVariantId} does not exist.");
        }

        if (!existingVariantIds.Contains(request.LensVariantId))
        {
            throw new InvalidOperationException($"Lens variant with ID {request.LensVariantId} does not exist.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var cart = await GetOrCreateCartAsync(customerId);
        var prescription = new Prescription
        {
            CustomerId = customerId,
            OdSphere = request.Prescription.OdSphere,
            OdCylinder = request.Prescription.OdCylinder,
            OdAxis = request.Prescription.OdAxis,
            OsSphere = request.Prescription.OsSphere,
            OsCylinder = request.Prescription.OsCylinder,
            OsAxis = request.Prescription.OsAxis,
            Pd = request.Prescription.Pd,
            Note = request.Prescription.Note,
            CreatedAt = DateTime.UtcNow,
            IsVerified = false,
            IsRejected = false
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        _context.CartItems.AddRange(
            new CartItem
            {
                CartId = cart.CartId,
                VariantId = request.FrameVariantId,
                PrescriptionId = prescription.PrescriptionId,
                Quantity = 1
            },
            new CartItem
            {
                CartId = cart.CartId,
                VariantId = request.LensVariantId,
                PrescriptionId = prescription.PrescriptionId,
                Quantity = 1
            }
        );

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetCartAsync(customerId);
    }

    public async Task<CartResponseDto> UpdateItemAsync(int cartItemId, int customerId, UpdateCartItemRequestDto request)
    {
        var cartItem = await _context.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

        if (cartItem == null)
        {
            throw new InvalidOperationException($"Cart item with ID {cartItemId} not found.");
        }

        if (cartItem.Cart.CustomerId != customerId)
        {
            throw new InvalidOperationException($"Cart item with ID {cartItemId} does not belong to this customer.");
        }

        cartItem.Quantity = request.Quantity;
        await _context.SaveChangesAsync();

        return await GetCartAsync(customerId);
    }

    public async Task<CartResponseDto> RemoveItemAsync(int cartItemId, int customerId)
    {
        var cartItem = await _context.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

        if (cartItem == null)
        {
            throw new InvalidOperationException($"Cart item with ID {cartItemId} not found.");
        }

        if (cartItem.Cart.CustomerId != customerId)
        {
            throw new InvalidOperationException($"Cart item with ID {cartItemId} does not belong to this customer.");
        }

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();

        return await GetCartAsync(customerId);
    }

    public async Task<bool> ClearCartAsync(int customerId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (cart == null)
        {
            return false;
        }

        _context.CartItems.RemoveRange(cart.CartItems);
        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<Cart> GetOrCreateCartAsync(int customerId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Variant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (cart != null)
        {
            return cart;
        }

        var now = DateTime.UtcNow;
        cart = new Cart
        {
            CustomerId = customerId,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        return cart;
    }

    private async Task<CartResponseDto> MapToCartResponseDtoAsync(Cart cart)
    {
        var now = DateTime.UtcNow;
        var items = new List<CartItemDto>();

        foreach (var ci in cart.CartItems)
        {
            var dto = new CartItemDto
            {
                CartItemId = ci.CartItemId,
                VariantId = ci.VariantId,
                ProductName = ci.Variant?.Product?.ProductName ?? string.Empty,
                VariantColor = ci.Variant?.Color,
                VariantSize = ci.Variant?.Size,
                Sku = ci.Variant?.Sku,
                Quantity = ci.Quantity,
                UnitPrice = (ci.Variant?.Product?.BasePrice ?? 0) + (ci.Variant?.AdditionalPrice ?? 0),
                StockQuantity = ci.Variant?.StockQuantity,
                IsPreOrder = ci.Variant?.Product?.IsPreOrder == true || (ci.Variant?.StockQuantity ?? 0) <= 0,
                PrescriptionId = ci.PrescriptionId
            };

            // Check for active pre-order campaign price
            var campaignPrice = await _context.PreOrderCampaignProducts
                .Include(cp => cp.Campaign)
                .Where(cp => 
                    cp.Campaign != null &&
                    cp.ProductId == (ci.Variant != null ? ci.Variant.ProductId : 0) &&
                    (cp.VariantId == null || cp.VariantId == ci.VariantId) &&
                    cp.Campaign.Status.ToLower() == "active" &&
                    cp.Campaign.StartDate <= now &&
                    cp.Campaign.EndDate >= now)
                .OrderByDescending(cp => cp.VariantId == ci.VariantId)
                .Select(cp => (decimal?)cp.CampaignPrice)
                .FirstOrDefaultAsync();

            dto.CampaignPrice = campaignPrice;
            items.Add(dto);
        }

        var totalAmount = items.Sum(i => (i.CampaignPrice ?? i.UnitPrice) * i.Quantity);

        return new CartResponseDto
        {
            CartId = cart.CartId,
            CustomerId = cart.CustomerId,
            Items = items,
            TotalAmount = totalAmount
        };
    }
}
