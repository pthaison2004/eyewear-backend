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

        return MapToCartResponseDto(cart);
    }

    public async Task<CartResponseDto> AddItemAsync(int customerId, AddCartItemRequestDto request)
    {
        var cart = await GetOrCreateCartAsync(customerId);

        var existingItem = cart.CartItems.FirstOrDefault(ci =>
            ci.VariantId == request.VariantId &&
            ci.PrescriptionId == request.PrescriptionId);

        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            var variantExists = await _context.ProductVariants.AnyAsync(v => v.VariantId == request.VariantId);
            if (!variantExists)
            {
                throw new InvalidOperationException($"Product variant with ID {request.VariantId} does not exist.");
            }

            var newItem = new CartItem
            {
                CartId = cart.CartId,
                VariantId = request.VariantId,
                PrescriptionId = request.PrescriptionId,
                Quantity = request.Quantity
            };
            _context.CartItems.Add(newItem);
        }

        await _context.SaveChangesAsync();

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

    private static CartResponseDto MapToCartResponseDto(Cart cart)
    {
        var items = cart.CartItems.Select(ci => new CartItemDto
        {
            CartItemId = ci.CartItemId,
            VariantId = ci.VariantId,
            ProductName = ci.Variant?.Product?.ProductName ?? string.Empty,
            VariantColor = ci.Variant?.Color,
            VariantSize = ci.Variant?.Size,
            Sku = ci.Variant?.Sku,
            Quantity = ci.Quantity,
            UnitPrice = (ci.Variant?.Product?.BasePrice ?? 0) + (ci.Variant?.AdditionalPrice ?? 0),
            PrescriptionId = ci.PrescriptionId
        }).ToList();

        var totalAmount = items.Sum(i => i.UnitPrice * i.Quantity);

        return new CartResponseDto
        {
            CartId = cart.CartId,
            CustomerId = cart.CustomerId,
            Items = items,
            TotalAmount = totalAmount
        };
    }
}