
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Participant")]
public class BrowseEventsController : Controller
{
	private readonly RaceDayApiClient _api;

	public BrowseEventsController(RaceDayApiClient api)
	{
		_api = api;
	}

	public async Task<IActionResult> Index(string? search, string? type)
	{
		using var response = await _api.GetAsync("api/events");

		if (response.StatusCode == HttpStatusCode.Unauthorized)
			return RedirectToAction("Login", "Account");

		if (!response.IsSuccessStatusCode)
			return StatusCode((int)response.StatusCode);

		var events = await response.Content
			.ReadFromJsonAsync<List<RaceEvent>>() ?? new();

		var upcoming = events
			.Where(e => e.EventDate >= DateTime.UtcNow);

		if (!string.IsNullOrWhiteSpace(search))
			upcoming = upcoming.Where(e =>
				e.EventName.Contains(search,
					StringComparison.OrdinalIgnoreCase) ||
				e.Location.Contains(search,
					StringComparison.OrdinalIgnoreCase));

		if (!string.IsNullOrWhiteSpace(type))
			upcoming = upcoming.Where(e =>
				e.EventType.Equals(type,
					StringComparison.OrdinalIgnoreCase));

		ViewBag.Search = search;
		ViewBag.Type = type;

		return View(upcoming.OrderBy(e => e.EventDate).ToList());
	}

	public async Task<IActionResult> Details(int id)
	{
		using var eventResponse =
			await _api.GetAsync($"api/events/{id}");

		if (eventResponse.StatusCode == HttpStatusCode.NotFound)
			return NotFound();

		if (!eventResponse.IsSuccessStatusCode)
			return StatusCode((int)eventResponse.StatusCode);

		var raceEvent = await eventResponse.Content
			.ReadFromJsonAsync<RaceEvent>();

		if (raceEvent == null)
			return NotFound();

		using var categoriesResponse =
			await _api.GetAsync($"api/events/{id}/categories");

		if (!categoriesResponse.IsSuccessStatusCode)
			return StatusCode((int)categoriesResponse.StatusCode);

		var categories = await categoriesResponse.Content
			.ReadFromJsonAsync<List<RaceEventCategory>>() ?? new();

		ViewBag.Categories = categories;
		return View(raceEvent);
	}
}
