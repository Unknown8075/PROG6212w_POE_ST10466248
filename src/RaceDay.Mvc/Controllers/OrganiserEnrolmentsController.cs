
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Organiser")]
public class OrganiserEnrolmentsController : Controller
{
	private readonly RaceDayApiClient _api;
	private readonly ILogger<OrganiserEnrolmentsController> _logger;

	public OrganiserEnrolmentsController(
		RaceDayApiClient api,
		ILogger<OrganiserEnrolmentsController> logger)
	{
		_api = api;
		_logger = logger;
	}

	public async Task<IActionResult> Index(int eventId)
	{
		if (eventId <= 0)
			return BadRequest();

		ViewBag.EventID = eventId;

		var model = new List<OrganiserEnrolmentViewModel>();

		try
		{
			using var response = await _api.GetAsync(
				$"api/enrolments/event/{eventId}");

			if (response.StatusCode == HttpStatusCode.Unauthorized)
				return RedirectToAction("Login", "Account");

			if (response.StatusCode == HttpStatusCode.Forbidden)
				return Forbid();

			if (response.StatusCode == HttpStatusCode.NotFound)
				return NotFound();

			if (!response.IsSuccessStatusCode)
			{
				TempData["Error"] = "Unable to load event enrolments.";
				return View(model);
			}

			model = await response.Content
				.ReadFromJsonAsync<List<OrganiserEnrolmentViewModel>>()
				?? new();
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Failed to load enrolments.");
			TempData["Error"] = "RaceDay API is unavailable.";
		}

		return View(model);
	}
}
