using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CmsController : ControllerBase
{
    private readonly VisionCareContext _context;

    public CmsController(VisionCareContext context)
    {
        _context = context;
    }

    [HttpGet("pages")]
    public async Task<IActionResult> GetPages()
    {
        try 
        {
            var pages = await _context.CmsPages.ToListAsync();
            return Ok(pages);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("pages")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePage([FromBody] CmsPage page)
    {
        try 
        {
            _context.CmsPages.Add(page);
            await _context.SaveChangesAsync();
            return Ok(page);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("pages/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePage(int id, [FromBody] CmsPage page)
    {
        try 
        {
            var existing = await _context.CmsPages.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Title = page.Title;
            existing.Slug = page.Slug;
            existing.Content = page.Content;
            
            await _context.SaveChangesAsync();
            return Ok(existing);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("pages/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePage(int id)
    {
        try 
        {
            var page = await _context.CmsPages.FindAsync(id);
            if (page == null) return NotFound();

            _context.CmsPages.Remove(page);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
