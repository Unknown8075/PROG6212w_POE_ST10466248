using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Organiser")]
public class EventsController : Controller
{
	private readonly RaceDayApiClient _api;
	private readonly ILogger<EventsController> _logger;

	public EventsController(
		RaceDayApiClient api,
		ILogger<EventsController> logger)
	{
		_api = api;
		_logger = logger;
	}

	private int CurrentUserId =>
		int.TryParse(
			User.FindFirstValue(ClaimTypes.NameIdentifier),
			out var id) ? id : -1;

	private async Task<IActionResult> HandleFailureAsync(
		HttpStatusCode status)
	{
		if (status == HttpStatusCode.Unauthorized)
		{
			HttpContext.Session.Clear();

			await HttpContext.SignOutAsync(
				CookieAuthenticationDefaults.AuthenticationScheme);

			return RedirectToAction("Login", "Account");
		}

		if (status == HttpStatusCode.Forbidden)
			return Forbid();

		TempData["Error"] = "Unable to complete the request.";
		return RedirectToAction(nameof(Index));
	}

	[HttpGet]
	public async Task<IActionResult> Index()
	{
		try
		{
			using var response = await _api.GetAsync("api/events");

			if (!response.IsSuccessStatusCode)
				return await HandleFailureAsync(response.StatusCode);

			var events = await response.Content
				.ReadFromJsonAsync<List<RaceEvent>>()
				?? new List<RaceEvent>();

			return View(events
				.Where(e => e.OrganiserID == CurrentUserId)
				.OrderBy(e => e.EventDate)
				.ToList());
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Unable to retrieve events.");
			TempData["Error"] = "RaceDay API is unavailable.";
			return View(new List<RaceEvent>());
		}
	}

	[HttpGet]
	public IActionResult Create()
	{
		return View(new EventFormViewModel());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(EventFormViewModel model)
	{
		if (!ModelState.IsValid)
			return View(model);

		try
		{
			using var response =
				await _api.PostAsync("api/events", model);

			if (!response.IsSuccessStatusCode)
			{
				if (response.StatusCode == HttpStatusCode.BadRequest)
				{
					ModelState.AddModelError("",
						"Check the event details. The date must be in the future.");
					return View(model);
				}

				return await HandleFailureAsync(response.StatusCode);
			}

			TempData["Success"] = "Event created successfully.";
			return RedirectToAction(nameof(Index));
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Unable to create event.");
			ModelState.AddModelError("",
				"The RaceDay API is unavailable.");
			return View(model);
		}
	}

	[HttpGet]
	public async Task<IActionResult> Details(int id)
	{
		try
		{
			using var response =
				await _api.GetAsync($"api/events/{id}");

			if (response.StatusCode == HttpStatusCode.NotFound)
				return NotFound();

			if (!response.IsSuccessStatusCode)
				return await HandleFailureAsync(response.StatusCode);

			var raceEvent = await response.Content
				.ReadFromJsonAsync<RaceEvent>();

			if (raceEvent == null)
				return NotFound();

			if (raceEvent.OrganiserID != CurrentUserId)
				return Forbid();

			return View(raceEvent);
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Unable to retrieve event {Id}.", id);
			TempData["Error"] = "Unable to load event.";
			return RedirectToAction(nameof(Index));
		}
	}

	[HttpGet]
	public async Task<IActionResult> Edit(int id)
	{
		try
		{
			using var response =
				await _api.GetAsync($"api/events/{id}");

			if (response.StatusCode == HttpStatusCode.NotFound)
				return NotFound();

			if (!response.IsSuccessStatusCode)
				return await HandleFailureAsync(response.StatusCode);

			var raceEvent = await response.Content
				.ReadFromJsonAsync<RaceEvent>();

			if (raceEvent == null)
				return NotFound();

			if (raceEvent.OrganiserID != CurrentUserId)
				return Forbid();

			return View(new EventFormViewModel
			{
				EventID = raceEvent.EventID,
				EventName = raceEvent.EventName,
				Description = raceEvent.Description,
				EventDate = raceEvent.EventDate,
				Location = raceEvent.Location,
				Distance = raceEvent.Distance,
				EventType = raceEvent.EventType
			});
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Unable to edit event {Id}.", id);
			TempData["Error"] = "Unable to load event.";
			return RedirectToAction(nameof(Index));
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(
		int id, EventFormViewModel model)
	{
		if (id != model.EventID)
			return BadRequest();

		if (!ModelState.IsValid)
			return View(model);

		try
		{
			using var response =
				await _api.PutAsync($"api/events/{id}", model);

			if (!response.IsSuccessStatusCode)
			{
				if (response.StatusCode == HttpStatusCode.BadRequest)
				{
					ModelState.AddModelError("",
						"Check the event details and date.");
					return View(model);
				}

				return await HandleFailureAsync(response.StatusCode);
			}

			TempData["Success"] = "Event updated successfully.";
			return RedirectToAction(nameof(Index));
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Unable to update event {Id}.", id);
			ModelState.AddModelError("",
				"The RaceDay API is unavailable.");
			return View(model);
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(int id)
	{
		try
		{
			using var response =
				await _api.DeleteAsync($"api/events/{id}");

			if (!response.IsSuccessStatusCode)
				return await HandleFailureAsync(response.StatusCode);

			TempData["Success"] = "Event deleted successfully.";
			return RedirectToAction(nameof(Index));
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Unable to delete event {Id}.", id);
			TempData["Error"] = "Unable to delete event.";
			return RedirectToAction(nameof(Index));
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[RequestSizeLimit(6 * 1024 * 1024)]
	public async Task<IActionResult> UploadBanner(
		int id,
		IFormFile? file)
	{
		if (id <= 0)
			return BadRequest();

		if (file == null || file.Length == 0)
		{
			TempData["Error"] = "Please select an event banner.";
			return RedirectToAction(nameof(Details), new { id });
		}

		if (file.Length > 5 * 1024 * 1024)
		{
			TempData["Error"] = "Banner must be 5 MB or smaller.";
			return RedirectToAction(nameof(Details), new { id });
		}

		var allowedExtensions = new[]
		{
		".jpg", ".jpeg", ".png", ".webp"
	};

		var extension = Path.GetExtension(file.FileName)
			.ToLowerInvariant();

		if (!allowedExtensions.Contains(extension))
		{
			TempData["Error"] =
				"Only JPG, PNG and WEBP images are supported.";

			return RedirectToAction(nameof(Details), new { id });
		}

		try
		{
			// Verify the event belongs to the logged-in organiser.
			using var eventResponse =
				await _api.GetAsync($"api/events/{id}");

			if (eventResponse.StatusCode == HttpStatusCode.NotFound)
				return NotFound();

			if (!eventResponse.IsSuccessStatusCode)
				return await HandleFailureAsync(eventResponse.StatusCode);

			var raceEvent = await eventResponse.Content
				.ReadFromJsonAsync<RaceEvent>();

			if (raceEvent == null)
				return NotFound();

			if (raceEvent.OrganiserID != CurrentUserId)
				return Forbid();

			// Forward the image to the RaceDay API.
			using var form = new MultipartFormDataContent();
			await using var stream = file.OpenReadStream();
			using var fileContent = new StreamContent(stream);

			var contentType = extension switch
			{
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".webp" => "image/webp",
				_ => "application/octet-stream"
			};

			fileContent.Headers.ContentType =
				new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);


			form.Add(
				fileContent,
				"file",
				Path.GetFileName(file.FileName));

			using var request = new HttpRequestMessage(
				HttpMethod.Post,
				$"api/images/events/{id}/banner")
			{
				Content = form
			};

			using var response = await _api.SendAsync(request);

			if (!response.IsSuccessStatusCode)
			{
				if (response.StatusCode == HttpStatusCode.Unauthorized ||
					response.StatusCode == HttpStatusCode.Forbidden)
				{
					return await HandleFailureAsync(response.StatusCode);
				}

				TempData["Error"] =
					"Banner upload failed. Check the API and Azurite.";

				return RedirectToAction(nameof(Details), new { id });
			}

			TempData["Success"] =
				"Event banner uploaded successfully.";

			return RedirectToAction(nameof(Details), new { id });
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(
				ex, "Unable to upload banner for event {Id}.", id);

			TempData["Error"] = "RaceDay API is unavailable.";

			return RedirectToAction(nameof(Details), new { id });
		}
	}

	[HttpGet]
	public async Task<IActionResult> Banner(int id)
	{
		if (id <= 0)
			return BadRequest();

		try
		{
			// Retrieve the event and verify organiser ownership.
			using var eventResponse =
				await _api.GetAsync($"api/events/{id}");

			if (eventResponse.StatusCode == HttpStatusCode.NotFound)
				return NotFound();

			if (!eventResponse.IsSuccessStatusCode)
			{
				if (eventResponse.StatusCode == HttpStatusCode.Unauthorized)
					return Unauthorized();

				if (eventResponse.StatusCode == HttpStatusCode.Forbidden)
					return Forbid();

				return StatusCode((int)eventResponse.StatusCode);
			}

			var raceEvent = await eventResponse.Content
				.ReadFromJsonAsync<RaceEvent>();

			if (raceEvent == null)
				return NotFound();

			if (raceEvent.OrganiserID != CurrentUserId)
				return Forbid();

			if (string.IsNullOrWhiteSpace(raceEvent.BannerImageUrl))
				return NotFound();

			// Fetch the image through the authenticated API client.
			using var response =
				await _api.GetAsync($"api/images/events/{id}/banner");

			if (response.StatusCode == HttpStatusCode.NotFound)
				return NotFound();

			if (response.StatusCode == HttpStatusCode.Unauthorized)
				return Unauthorized();

			if (response.StatusCode == HttpStatusCode.Forbidden)
				return Forbid();

			if (!response.IsSuccessStatusCode)
				return StatusCode((int)response.StatusCode);

			var contentType =
				response.Content.Headers.ContentType?.MediaType;

			if (contentType != "image/jpeg" &&
				contentType != "image/png" &&
				contentType != "image/webp")
			{
				return StatusCode(502);
			}

			var bytes = await response.Content.ReadAsByteArrayAsync();

			return File(bytes, contentType);
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(
				ex,
				"Unable to retrieve banner for event {Id}.",
				id);

			return StatusCode(503);
		}
	}
}
