using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/enrolments")]
[Authorize]
public class EnrolmentsController : ControllerBase
{
	private readonly RaceDayDbContext _context;

	public EnrolmentsController(RaceDayDbContext context)
	{
		_context = context;
	}

	// POST /api/enrolments
	// Participants enrol in an event category.
	[HttpPost]
	[Authorize(Roles = "Participant")]
	public async Task<IActionResult> CreateEnrolment(
		[FromBody] EnrolmentRequest request)
	{
		int? participantId = GetUserId();

		if (participantId == null)
			return Unauthorized();

		// A serializable transaction protects the capacity check
		// against concurrent registrations.
		await using var transaction =
			await _context.Database.BeginTransactionAsync(
				IsolationLevel.Serializable);

		var eventCategory = await _context.EventCategories
			.AsNoTracking()
			.FirstOrDefaultAsync(ec =>
				ec.EventCategoryID == request.EventCategoryID);

		if (eventCategory == null)
		{
			return NotFound(new
			{
				message = "Event category not found."
			});
		}

		var existing = await _context.Enrolments
			.FirstOrDefaultAsync(e =>
				e.ParticipantID == participantId.Value &&
				e.EventCategoryID == request.EventCategoryID);

		if (existing != null)
		{
			return Conflict(new
			{
				message = existing.Status == "Cancelled"
					? "An enrolment already exists in Cancelled status."
					: "You are already enrolled in this category."
			});
		}

		int activeCount = await _context.Enrolments
			.CountAsync(e =>
				e.EventCategoryID == request.EventCategoryID &&
				e.Status != "Cancelled");

		if (activeCount >= eventCategory.MaximumParticipants)
		{
			return Conflict(new
			{
				message = "This event category is full."
			});
		}

		var enrolment = new Enrolment
		{
			ParticipantID = participantId.Value,
			EventCategoryID = request.EventCategoryID,
			EnrolmentDate = DateTime.UtcNow,
			Status = "Pending"
		};

		_context.Enrolments.Add(enrolment);

		await _context.SaveChangesAsync();
		await transaction.CommitAsync();

		return StatusCode(201, new
		{
			message = "Enrolment created successfully.",
			enrolment.EnrolmentID,
			enrolment.EventCategoryID,
			enrolment.Status,
			enrolment.EnrolmentDate
		});
	}

	// GET /api/enrolments/my
	// Participants see only their own enrolments.
	[HttpGet("my")]
	[Authorize(Roles = "Participant")]
	public async Task<IActionResult> GetMyEnrolments()
	{
		int? participantId = GetUserId();

		if (participantId == null)
			return Unauthorized();

		var enrolments = await _context.Enrolments
			.AsNoTracking()
			.Where(e => e.ParticipantID == participantId.Value)
			.OrderByDescending(e => e.EnrolmentDate)
			.Select(e => new
			{
				e.EnrolmentID,
				e.EventCategoryID,
				e.EventCategory.EventID,
				e.EventCategory.Event.EventName,
				e.EventCategory.Category.CategoryName,
				e.EnrolmentDate,
				e.Status
			})
			.ToListAsync();

		return Ok(enrolments);
	}

	// GET /api/enrolments/{id}
	// Participants can access only their own records.
	[HttpGet("{id:int}")]
	[Authorize(Roles = "Participant")]
	public async Task<IActionResult> GetEnrolment(int id)
	{
		int? participantId = GetUserId();

		if (participantId == null)
			return Unauthorized();

		var enrolment = await _context.Enrolments
			.AsNoTracking()
			.Where(e =>
				e.EnrolmentID == id &&
				e.ParticipantID == participantId.Value)
			.Select(e => new
			{
				e.EnrolmentID,
				e.EventCategoryID,
				e.EnrolmentDate,
				e.Status
			})
			.FirstOrDefaultAsync();

		if (enrolment == null)
		{
			return NotFound(new
			{
				message = "Enrolment not found."
			});
		}

		return Ok(enrolment);
	}

	// PUT /api/enrolments/{id}/cancel
	// Participants cancel their own enrolments.
	[HttpPut("{id:int}/cancel")]
	[Authorize(Roles = "Participant")]
	public async Task<IActionResult> CancelEnrolment(int id)
	{
		int? participantId = GetUserId();

		if (participantId == null)
			return Unauthorized();

		var enrolment = await _context.Enrolments
			.FirstOrDefaultAsync(e =>
				e.EnrolmentID == id &&
				e.ParticipantID == participantId.Value);

		if (enrolment == null)
		{
			return NotFound(new
			{
				message = "Enrolment not found."
			});
		}

		if (enrolment.Status == "Cancelled")
		{
			return Conflict(new
			{
				message = "Enrolment is already cancelled."
			});
		}

		enrolment.Status = "Cancelled";

		await _context.SaveChangesAsync();

		return Ok(new
		{
			message = "Enrolment cancelled successfully.",
			enrolment.EnrolmentID,
			enrolment.Status
		});
	}

	private int? GetUserId()
	{
		var claim = User.FindFirstValue(
			ClaimTypes.NameIdentifier);

		return int.TryParse(claim, out int userId)
			? userId
			: null;
	}
}
