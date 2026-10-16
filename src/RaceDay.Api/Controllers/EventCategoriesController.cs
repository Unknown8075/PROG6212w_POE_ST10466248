
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/categories")]
[Authorize]
public class EventCategoriesController : ControllerBase
{
	private readonly RaceDayDbContext _context;

	public EventCategoriesController(RaceDayDbContext context)
	{
		_context = context;
	}

	// GET: api/events/2/categories
	[HttpGet]
	public async Task<IActionResult> GetEventCategories(int eventId)
	{
		bool eventExists = await _context.Events
			.AnyAsync(e => e.EventID == eventId);

		if (!eventExists)
		{
			return NotFound(new { message = "Event not found." });
		}

		var categories = await _context.EventCategories
			.AsNoTracking()
			.Where(ec => ec.EventID == eventId)
			.Select(ec => new
			{
				ec.EventCategoryID,
				ec.EventID,
				ec.CategoryID,
				ec.Category.CategoryName,
				ec.EntryFee,
				ec.MaximumParticipants
			})
			.ToListAsync();

		return Ok(categories);
	}

	// POST: api/events/2/categories
	[HttpPost]
	[Authorize(Roles = "Organiser")]
	public async Task<IActionResult> AssignCategory(
		int eventId,
		[FromBody] EventCategoryRequest request)
	{
		var userId = GetUserId();

		if (userId == null)
			return Unauthorized();

		var eventEntity = await _context.Events.FindAsync(eventId);

		if (eventEntity == null)
			return NotFound(new { message = "Event not found." });

		if (eventEntity.OrganiserID != userId.Value)
			return Forbid();

		bool categoryExists = await _context.Categories
			.AnyAsync(c => c.CategoryID == request.CategoryID);

		if (!categoryExists)
			return NotFound(new { message = "Category not found." });

		bool alreadyAssigned = await _context.EventCategories
			.AnyAsync(ec =>
				ec.EventID == eventId &&
				ec.CategoryID == request.CategoryID);

		if (alreadyAssigned)
		{
			return Conflict(new
			{
				message = "Category already assigned to this event."
			});
		}

		var eventCategory = new EventCategory
		{
			EventID = eventId,
			CategoryID = request.CategoryID,
			EntryFee = request.EntryFee,
			MaximumParticipants = request.MaximumParticipants
		};

		_context.EventCategories.Add(eventCategory);
		await _context.SaveChangesAsync();

		return StatusCode(201, new
		{
			message = "Category assigned successfully.",
			eventCategory.EventCategoryID,
			eventCategory.EventID,
			eventCategory.CategoryID,
			eventCategory.EntryFee,
			eventCategory.MaximumParticipants
		});
	}

	// PUT: api/events/2/categories/1
	[HttpPut("{eventCategoryId:int}")]
	[Authorize(Roles = "Organiser")]
	public async Task<IActionResult> UpdateEventCategory(
		int eventId,
		int eventCategoryId,
		[FromBody] EventCategoryRequest request)
	{
		var userId = GetUserId();

		if (userId == null)
			return Unauthorized();

		var eventEntity = await _context.Events.FindAsync(eventId);

		if (eventEntity == null)
			return NotFound(new { message = "Event not found." });

		if (eventEntity.OrganiserID != userId.Value)
			return Forbid();

		var eventCategory = await _context.EventCategories
			.FirstOrDefaultAsync(ec =>
				ec.EventCategoryID == eventCategoryId &&
				ec.EventID == eventId);

		if (eventCategory == null)
		{
			return NotFound(new
			{
				message = "Event category not found."
			});
		}

		bool categoryExists = await _context.Categories
			.AnyAsync(c => c.CategoryID == request.CategoryID);

		if (!categoryExists)
			return NotFound(new { message = "Category not found." });

		bool duplicate = await _context.EventCategories
			.AnyAsync(ec =>
				ec.EventID == eventId &&
				ec.CategoryID == request.CategoryID &&
				ec.EventCategoryID != eventCategoryId);

		if (duplicate)
		{
			return Conflict(new
			{
				message = "Category already assigned to this event."
			});
		}

		int activeEnrolments = await _context.Enrolments
			.CountAsync(e =>
				e.EventCategoryID == eventCategoryId &&
				e.Status != "Cancelled");

		if (request.MaximumParticipants < activeEnrolments)
		{
			return Conflict(new
			{
				message = "Capacity cannot be below active enrolments."
			});
		}

		eventCategory.CategoryID = request.CategoryID;
		eventCategory.EntryFee = request.EntryFee;
		eventCategory.MaximumParticipants = request.MaximumParticipants;

		await _context.SaveChangesAsync();

		return Ok(new
		{
			message = "Event category updated successfully."
		});
	}

	// DELETE: api/events/2/categories/1
	[HttpDelete("{eventCategoryId:int}")]
	[Authorize(Roles = "Organiser")]
	public async Task<IActionResult> RemoveEventCategory(
		int eventId,
		int eventCategoryId)
	{
		var userId = GetUserId();

		if (userId == null)
			return Unauthorized();

		var eventEntity = await _context.Events.FindAsync(eventId);

		if (eventEntity == null)
			return NotFound(new { message = "Event not found." });

		if (eventEntity.OrganiserID != userId.Value)
			return Forbid();

		var eventCategory = await _context.EventCategories
			.FirstOrDefaultAsync(ec =>
				ec.EventCategoryID == eventCategoryId &&
				ec.EventID == eventId);

		if (eventCategory == null)
		{
			return NotFound(new
			{
				message = "Event category not found."
			});
		}

		bool hasEnrolments = await _context.Enrolments
			.AnyAsync(e =>
				e.EventCategoryID == eventCategoryId);

		if (hasEnrolments)
		{
			return Conflict(new
			{
				message = "Cannot remove a category with enrolment records."
			});
		}

		_context.EventCategories.Remove(eventCategory);
		await _context.SaveChangesAsync();

		return Ok(new
		{
			message = "Category removed from event successfully."
		});
	}

	private int? GetUserId()
	{
		var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

		return int.TryParse(claim, out int userId)
			? userId
			: null;
	}
}
