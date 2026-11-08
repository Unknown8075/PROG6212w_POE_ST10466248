
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Organiser")]
public class OrganiserController : Controller
{
	private readonly RaceDayApiClient _api;
	private readonly ILogger<OrganiserController> _logger;

	public OrganiserController(
		RaceDayApiClient api,
		ILogger<OrganiserController> logger)
	{
		_api = api;
		_logger = logger;
	}

	public async Task<IActionResult> Index()
	{
		var model = new OrganiserDashboardViewModel();

		if (!int.TryParse(
			User.FindFirstValue(ClaimTypes.NameIdentifier),
			out int organiserId))
		{
			return Challenge();
		}

		try
		{
			using var response =
				await _api.GetAsync("api/events");

			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				HttpContext.Session.Clear();
				return RedirectToAction("Login", "Account");
			}

			if (!response.IsSuccessStatusCode)
			{
				TempData["Error"] =
					"Unable to load events at the moment.";

				return View(model);
			}

			var events = await response.Content
				.ReadFromJsonAsync<List<RaceEvent>>()
				?? new List<RaceEvent>();

			model.Events = events
				.Where(e => e.OrganiserID == organiserId)
				.OrderBy(e => e.EventDate)
				.ToList();
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Failed to load organiser events.");

			TempData["Error"] =
				"The RaceDay API is currently unavailable.";
		}

		return View(model);
	}
}
