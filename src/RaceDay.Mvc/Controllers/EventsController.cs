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
}
