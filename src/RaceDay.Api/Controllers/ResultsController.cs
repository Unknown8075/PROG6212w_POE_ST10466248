using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/results")]
[Authorize]
public class ResultsController : ControllerBase
{
	private readonly RaceDayDbContext _context;

	public ResultsController(RaceDayDbContext context)
	{
		_context = context;
	}

	// POST /api/results
	[HttpPost]
	[Authorize(Roles = "Organiser")]
	public async Task<IActionResult> CreateResult(
		[FromBody] ResultRequest request)
	{
		int? userId = GetUserId();

		if (userId == null)
			return Unauthorized();

		var enrolment = await _context.Enrolments
			.Include(e => e.EventCategory)
			.FirstOrDefaultAsync(e =>
				e.EnrolmentID == request.EnrolmentID);

		if (enrolment == null)
			return NotFound(new { message = "Enrolment not found." });

		bool ownsEvent = await _context.Events.AnyAsync(e =>
			e.EventID == enrolment.EventCategory.EventID &&
			e.OrganiserID == userId.Value);

		if (!ownsEvent)
			return Forbid();

		if (enrolment.Status == "Cancelled")
		{
			return Conflict(new
			{
				message = "Cannot record a result for a cancelled enrolment."
			});
		}

		bool resultExists = await _context.Results
			.AnyAsync(r => r.EnrolmentID == request.EnrolmentID);

		if (resultExists)
		{
			return Conflict(new
			{
				message = "A result already exists for this enrolment."
			});
		}

		if (!IsValidResult(request))
		{
			return BadRequest(new
			{
				message = "Invalid finishing time or position for result status."
			});
		}

		var result = new Result
		{
			EnrolmentID = request.EnrolmentID,
			FinishTime = request.FinishTime,
			Position = request.Position,
			ResultStatus = request.ResultStatus
		};

		_context.Results.Add(result);
		await _context.SaveChangesAsync();

		return StatusCode(201, new
		{
			message = "Result created successfully.",
			result.ResultID,
			result.EnrolmentID,
			result.FinishTime,
			result.Position,
			result.ResultStatus
		});
	}

	// GET /api/results/my
	[HttpGet("my")]
	[Authorize(Roles = "Participant")]
	public async Task<IActionResult> GetMyResults()
	{
		int? userId = GetUserId();

		if (userId == null)
			return Unauthorized();

		var results = await _context.Results
			.AsNoTracking()
			.Where(r => r.Enrolment.ParticipantID == userId.Value)
			.Select(r => new
			{
				r.ResultID,
				r.EnrolmentID,
				r.Enrolment.EventCategory.EventID,
				r.Enrolment.EventCategory.Event.EventName,
				r.FinishTime,
				r.Position,
				r.ResultStatus
			})
			.ToListAsync();

		return Ok(results);
	}

	// GET /api/results/{id}
	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetResult(int id)
	{
		int? userId = GetUserId();

		if (userId == null)
			return Unauthorized();

		var result = await _context.Results
			.AsNoTracking()
			.Where(r => r.ResultID == id)
			.Select(r => new
			{
				r.ResultID,
				r.EnrolmentID,
				r.Enrolment.ParticipantID,
				r.Enrolment.EventCategory.Event.OrganiserID,
				r.FinishTime,
				r.Position,
				r.ResultStatus
			})
			.FirstOrDefaultAsync();

		if (result == null)
			return NotFound(new { message = "Result not found." });

		if (result.ParticipantID != userId.Value &&
			result.OrganiserID != userId.Value)
		{
			return Forbid();
		}

		return Ok(result);
	}

	// PUT /api/results/{id}
	[HttpPut("{id:int}")]
	[Authorize(Roles = "Organiser")]
	public async Task<IActionResult> UpdateResult(
		int id,
		[FromBody] ResultRequest request)
	{
		int? userId = GetUserId();

		if (userId == null)
			return Unauthorized();

		var result = await _context.Results
			.Include(r => r.Enrolment)
			.ThenInclude(e => e.EventCategory)
			.FirstOrDefaultAsync(r => r.ResultID == id);

		if (result == null)
			return NotFound(new { message = "Result not found." });

		bool ownsEvent = await _context.Events.AnyAsync(e =>
			e.EventID == result.Enrolment.EventCategory.EventID &&
			e.OrganiserID == userId.Value);

		if (!ownsEvent)
			return Forbid();

		if (result.EnrolmentID != request.EnrolmentID)
		{
			return BadRequest(new
			{
				message = "Cannot change the enrolment linked to a result."
			});
		}

		if (!IsValidResult(request))
		{
			return BadRequest(new
			{
				message = "Invalid finishing time or position for result status."
			});
		}

		result.FinishTime = request.FinishTime;
		result.Position = request.Position;
		result.ResultStatus = request.ResultStatus;

		await _context.SaveChangesAsync();

		return Ok(new
		{
			message = "Result updated successfully.",
			result.ResultID,
			result.ResultStatus
		});
	}

	// GET /api/results/event/2
	[HttpGet("event/{eventId:int}")]
	[Authorize(Roles = "Organiser")]
	public async Task<IActionResult> GetEventResults(int eventId)
	{
		int? userId = GetUserId();

		if (userId == null)
			return Unauthorized();

		var eventEntity = await _context.Events.FindAsync(eventId);

		if (eventEntity == null)
			return NotFound(new { message = "Event not found." });

		if (eventEntity.OrganiserID != userId.Value)
			return Forbid();

		var results = await _context.Results
			.AsNoTracking()
			.Where(r =>
				r.Enrolment.EventCategory.EventID == eventId)
			.OrderBy(r => r.Position)
			.Select(r => new
			{
				r.ResultID,
				r.EnrolmentID,
				r.Enrolment.ParticipantID,
				r.FinishTime,
				r.Position,
				r.ResultStatus
			})
			.ToListAsync();

		return Ok(results);
	}

	private static bool IsValidResult(ResultRequest request)
	{
		if (request.ResultStatus == "Finished")
		{
			return request.FinishTime.HasValue &&
				   request.FinishTime.Value > TimeSpan.Zero &&
				   request.Position.HasValue &&
				   request.Position.Value > 0;
		}

		return request.FinishTime == null &&
			   request.Position == null;
	}

	private int? GetUserId()
	{
		var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

		return int.TryParse(claim, out int userId)
			? userId
			: null;
	}
}
