using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
	private readonly RaceDayDbContext _context;

	public CategoriesController(RaceDayDbContext context)
	{
		_context = context;
	}

	// GET: api/categories
	[HttpGet]
	public async Task<IActionResult> GetCategories()
	{
		var categories = await _context.Categories
			.AsNoTracking()
			.OrderBy(c => c.CategoryName)
			.Select(c => new
			{
				c.CategoryID,
				c.CategoryName,
				c.Description
			})
			.ToListAsync();

		return Ok(categories);
	}

	// GET: api/categories/1
	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetCategory(int id)
	{
		var category = await _context.Categories
			.AsNoTracking()
			.Where(c => c.CategoryID == id)
			.Select(c => new
			{
				c.CategoryID,
				c.CategoryName,
				c.Description
			})
			.FirstOrDefaultAsync();

		if (category == null)
			return NotFound(new { message = "Category not found." });

		return Ok(category);
	}

	// POST: api/categories
	[HttpPost]
	[Authorize(Roles = "Organiser")]
	public async Task<IActionResult> CreateCategory(
		[FromBody] CategoryRequest request)
	{
		var name = request.CategoryName.Trim();

		if (name.Length < 2)
			return BadRequest(new { message = "Invalid category name." });

		bool exists = await _context.Categories
			.AnyAsync(c => c.CategoryName == name);

		if (exists)
			return Conflict(new { message = "Category already exists." });

		var category = new Category
		{
			CategoryName = name,
			Description = request.Description?.Trim()
		};

		_context.Categories.Add(category);
		await _context.SaveChangesAsync();

		return CreatedAtAction(
			nameof(GetCategory),
			new { id = category.CategoryID },
			new
			{
				category.CategoryID,
				category.CategoryName,
				category.Description
			});
	}

	// PUT: api/categories/1
	[HttpPut("{id:int}")]
	[Authorize(Roles = "Organiser")]
	public async Task<IActionResult> UpdateCategory(
		int id,
		[FromBody] CategoryRequest request)
	{
		var category = await _context.Categories.FindAsync(id);

		if (category == null)
			return NotFound(new { message = "Category not found." });

		var name = request.CategoryName.Trim();

		if (name.Length < 2)
			return BadRequest(new { message = "Invalid category name." });

		bool duplicate = await _context.Categories
			.AnyAsync(c => c.CategoryID != id &&
						   c.CategoryName == name);

		if (duplicate)
			return Conflict(new { message = "Category name already exists." });

		category.CategoryName = name;
		category.Description = request.Description?.Trim();

		await _context.SaveChangesAsync();

		return Ok(new
		{
			message = "Category updated successfully.",
			category.CategoryID,
			category.CategoryName,
			category.Description
		});
	}

	// DELETE: api/categories/1
	[HttpDelete("{id:int}")]
	[Authorize(Roles = "Organiser")]
	public async Task<IActionResult> DeleteCategory(int id)
	{
		var category = await _context.Categories
			.Include(c => c.EventCategories)
			.FirstOrDefaultAsync(c => c.CategoryID == id);

		if (category == null)
			return NotFound(new { message = "Category not found." });

		if (category.EventCategories.Any())
			return Conflict(new
			{
				message = "Cannot delete a category assigned to an event."
			});

		_context.Categories.Remove(category);
		await _context.SaveChangesAsync();

		return Ok(new { message = "Category deleted successfully." });
	}
}
