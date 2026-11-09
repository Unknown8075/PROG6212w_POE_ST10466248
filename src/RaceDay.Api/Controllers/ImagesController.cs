
using System.Security.Claims;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/images")]
[Authorize]
public class ImagesController : ControllerBase
{
	private readonly RaceDayDbContext _db;
	private readonly AzureBlobStorageService _storage;

	public ImagesController(
		RaceDayDbContext db,
		AzureBlobStorageService storage)
	{
		_db = db;
		_storage = storage;
	}

	private int CurrentUserId =>
		int.TryParse(
			User.FindFirstValue(ClaimTypes.NameIdentifier),
			out var id) ? id : -1;

	[HttpPost("events/{eventId:int}/banner")]
	[Authorize(Roles = "Organiser")]
	[RequestSizeLimit(6 * 1024 * 1024)]
	public async Task<IActionResult> UploadEventBanner(
		int eventId,
		[FromForm] IFormFile file)
	{
		var raceEvent = await _db.Events
			.FirstOrDefaultAsync(e => e.EventID == eventId);

		if (raceEvent == null)
			return NotFound(new { message = "Event not found." });

		if (raceEvent.OrganiserID != CurrentUserId)
			return Forbid();

		if (file == null)
			return BadRequest(new { message = "Select an image." });

		try
		{
			raceEvent.BannerImageUrl = await _storage.UploadAsync(
				file, "event-banners");

			await _db.SaveChangesAsync();

			return Ok(new
			{
				message = "Event banner uploaded successfully.",
				raceEvent.EventID,
				raceEvent.BannerImageUrl
			});
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
		catch (RequestFailedException)
		{
			return StatusCode(503,
				new { message = "Image storage is unavailable." });
		}
	}

	[HttpGet("events/{eventId:int}/banner")]
	public async Task<IActionResult> GetEventBanner(int eventId)
	{
		var imageUrl = await _db.Events
			.Where(e => e.EventID == eventId)
			.Select(e => e.BannerImageUrl)
			.FirstOrDefaultAsync();

		if (string.IsNullOrWhiteSpace(imageUrl))
			return NotFound();

		try
		{
			var image = await _storage.DownloadAsync(
				imageUrl, "event-banners");

			return File(image.Content, image.ContentType);
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			return NotFound();
		}
		catch (RequestFailedException)
		{
			return StatusCode(503);
		}
		catch (ArgumentException)
		{
			return StatusCode(500);
		}
	}

	[HttpPost("profile/picture")]
	[Authorize(Roles = "Participant")]
	[RequestSizeLimit(6 * 1024 * 1024)]
	public async Task<IActionResult> UploadProfilePicture(
		[FromForm] IFormFile file)
	{
		var user = await _db.Users
			.FirstOrDefaultAsync(u => u.UserID == CurrentUserId);

		if (user == null)
			return NotFound();

		if (file == null)
			return BadRequest(new { message = "Select an image." });

		try
		{
			user.ProfilePictureUrl = await _storage.UploadAsync(
				file, "profile-pictures");

			await _db.SaveChangesAsync();

			return Ok(new
			{
				message = "Profile picture uploaded successfully.",
				user.ProfilePictureUrl
			});
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
		catch (RequestFailedException)
		{
			return StatusCode(503,
				new { message = "Image storage is unavailable." });
		}
	}

	[HttpGet("profile/picture")]
	[Authorize(Roles = "Participant")]
	public async Task<IActionResult> GetProfilePicture()
	{
		var imageUrl = await _db.Users
			.Where(u => u.UserID == CurrentUserId)
			.Select(u => u.ProfilePictureUrl)
			.FirstOrDefaultAsync();

		if (string.IsNullOrWhiteSpace(imageUrl))
			return NotFound();

		try
		{
			var image = await _storage.DownloadAsync(
				imageUrl, "profile-pictures");

			return File(image.Content, image.ContentType);
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			return NotFound();
		}
		catch (RequestFailedException)
		{
			return StatusCode(503);
		}
		catch (ArgumentException)
		{
			return StatusCode(500);
		}
	}
}
