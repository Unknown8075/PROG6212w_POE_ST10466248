using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // GET: api/events
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _context.Events
            .AsNoTracking()
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventID,
                e.EventName,
                e.Description,
                e.EventDate,
                e.Location,
                e.Distance,
                e.EventType,
                e.OrganiserID
            })
            .ToListAsync();

        return Ok(events);
    }

    // GET: api/events/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var raceEvent = await _context.Events
            .AsNoTracking()
            .Where(e => e.EventID == id)
            .Select(e => new
            {
                e.EventID,
                e.EventName,
                e.Description,
                e.EventDate,
                e.Location,
                e.Distance,
                e.EventType,
                e.OrganiserID
            })
            .FirstOrDefaultAsync();

        if (raceEvent == null)
            return NotFound(new { message = "Event not found." });

        return Ok(raceEvent);
    }

    // POST: api/events
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateEvent(
        [FromBody] EventRequest request)
    {
        if (!TryGetUserId(out int organiserId))
            return Unauthorized();

        if (!ValidEventDetails(request))
            return BadRequest(new
            {
                message = "Event date must be in the future and text fields cannot be blank."
            });

        var raceEvent = new RaceDay.Api.Models.Event
        {
            OrganiserID = organiserId,
            EventName = request.EventName.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            EventType = request.EventType
        };

        _context.Events.Add(raceEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEvent),
            new { id = raceEvent.EventID },
            new
            {
                raceEvent.EventID,
                raceEvent.EventName,
                raceEvent.OrganiserID
            });
    }

    // PUT: api/events/1
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateEvent(
        int id, [FromBody] EventRequest request)
    {
        if (!TryGetUserId(out int organiserId))
            return Unauthorized();

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (raceEvent == null)
            return NotFound(new { message = "Event not found." });

        if (raceEvent.OrganiserID != organiserId)
            return Forbid();

        if (!ValidEventDetails(request))
            return BadRequest(new
            {
                message = "Event date must be in the future and text fields cannot be blank."
            });

        raceEvent.EventName = request.EventName.Trim();
        raceEvent.Description = request.Description.Trim();
        raceEvent.EventDate = request.EventDate;
        raceEvent.Location = request.Location.Trim();
        raceEvent.Distance = request.Distance;
        raceEvent.EventType = request.EventType;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Event updated successfully.",
            raceEvent.EventID
        });
    }

    // DELETE: api/events/1
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        if (!TryGetUserId(out int organiserId))
            return Unauthorized();

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (raceEvent == null)
            return NotFound(new { message = "Event not found." });

        if (raceEvent.OrganiserID != organiserId)
            return Forbid();

        _context.Events.Remove(raceEvent);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Event deleted successfully." });
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }

    private static bool ValidEventDetails(EventRequest request)
    {
        return request.EventDate > DateTime.UtcNow
            && !string.IsNullOrWhiteSpace(request.EventName)
            && !string.IsNullOrWhiteSpace(request.Description)
            && !string.IsNullOrWhiteSpace(request.Location);
    }
}
