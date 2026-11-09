
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Organiser")]
public class OrganiserResultsController : Controller
{
	private readonly RaceDayApiClient _api;

	public OrganiserResultsController(RaceDayApiClient api)
	{
		_api = api;
	}

	private async Task<IActionResult> ApiFailure(
		HttpResponseMessage response)
	{
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			HttpContext.Session.Clear();

			await HttpContext.SignOutAsync(
				CookieAuthenticationDefaults.AuthenticationScheme);

			return RedirectToAction("Login", "Account");
		}

		if (response.StatusCode == HttpStatusCode.Forbidden)
			return Forbid();

		if (response.StatusCode == HttpStatusCode.NotFound)
			return NotFound();

		TempData["Error"] = "Unable to complete the API request.";
		return RedirectToAction("Index", "Events");
	}

	[HttpGet]
	public async Task<IActionResult> Index(int eventId)
	{
		if (eventId <= 0)
			return BadRequest();

		try
		{
			using var response = await _api.GetAsync(
				$"api/results/event/{eventId}");

			if (!response.IsSuccessStatusCode)
				return await ApiFailure(response);

			var results = await response.Content
				.ReadFromJsonAsync<List<OrganiserRaceResult>>()
				?? new();

			ViewBag.EventID = eventId;
			return View(results);
		}
		catch (HttpRequestException)
		{
			TempData["Error"] = "RaceDay API is unavailable.";
			return RedirectToAction("Index", "Events");
		}
	}

	[HttpGet]
	public async Task<IActionResult> Create(
		int eventId, int enrolmentId)
	{
		if (eventId <= 0 || enrolmentId <= 0)
			return BadRequest();

		try
		{
			using var response = await _api.GetAsync(
				$"api/enrolments/event/{eventId}");

			if (!response.IsSuccessStatusCode)
				return await ApiFailure(response);

			var enrolments = await response.Content
				.ReadFromJsonAsync<List<OrganiserEnrolmentViewModel>>()
				?? new();

			var enrolment = enrolments.FirstOrDefault(
				e => e.EnrolmentID == enrolmentId);

			if (enrolment == null)
				return NotFound();

			if (enrolment.Status == "Cancelled")
				return BadRequest("Cannot record a cancelled enrolment.");

			return View("Form", new RaceResultFormViewModel
			{
				EventID = eventId,
				EnrolmentID = enrolmentId
			});
		}
		catch (HttpRequestException)
		{
			TempData["Error"] = "RaceDay API is unavailable.";
			return RedirectToAction("Index", "Events");
		}
	}

	[HttpGet]
	public async Task<IActionResult> Edit(int eventId, int id)
	{
		if (eventId <= 0 || id <= 0)
			return BadRequest();

		try
		{
			// Verify the result belongs to an event this organiser owns.
			using var eventResponse = await _api.GetAsync(
				$"api/results/event/{eventId}");

			if (!eventResponse.IsSuccessStatusCode)
				return await ApiFailure(eventResponse);

			var eventResults = await eventResponse.Content
				.ReadFromJsonAsync<List<OrganiserRaceResult>>()
				?? new();

			if (!eventResults.Any(r => r.ResultID == id))
				return NotFound();

			using var response = await _api.GetAsync(
				$"api/results/{id}");

			if (!response.IsSuccessStatusCode)
				return await ApiFailure(response);

			var result = await response.Content
				.ReadFromJsonAsync<OrganiserRaceResult>();

			if (result == null)
				return NotFound();

			return View("Form", new RaceResultFormViewModel
			{
				EventID = eventId,
				ResultID = result.ResultID,
				EnrolmentID = result.EnrolmentID,
				FinishTimeText = result.FinishTime?.ToString(
					"c", CultureInfo.InvariantCulture),
				Position = result.Position,
				ResultStatus = result.ResultStatus
			});
		}
		catch (HttpRequestException)
		{
			TempData["Error"] = "RaceDay API is unavailable.";
			return RedirectToAction("Index", "Events");
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Save(
		RaceResultFormViewModel model)
	{
		if (model.EventID <= 0 || model.ResultID < 0)
			return BadRequest();

		TimeSpan? finishTime = null;
		int? position = null;

		if (model.ResultStatus == "Finished")
		{
			if (!TimeSpan.TryParseExact(
				model.FinishTimeText,
				"c",
				CultureInfo.InvariantCulture,
				out var parsedTime) ||
				parsedTime <= TimeSpan.Zero)
			{
				ModelState.AddModelError(
					nameof(model.FinishTimeText),
					"Enter a positive time, e.g. 00:45:30.");
			}
			else
			{
				finishTime = parsedTime;
			}

			if (!model.Position.HasValue ||
				model.Position.Value <= 0)
			{
				ModelState.AddModelError(
					nameof(model.Position),
					"Enter a valid finishing position.");
			}
			else
			{
				position = model.Position.Value;
			}
		}
		else if (!string.IsNullOrWhiteSpace(model.FinishTimeText) ||
				 model.Position.HasValue)
		{
			ModelState.AddModelError(
				"",
				"DNF, DNS and DQ must have no finish time or position.");
		}

		if (!ModelState.IsValid)
			return View("Form", model);

		var request = new
		{
			model.EnrolmentID,
			FinishTime = finishTime,
			Position = position,
			model.ResultStatus
		};

		try
		{
			using var response = model.ResultID == 0
				? await _api.PostAsync("api/results", request)
				: await _api.PutAsync(
					$"api/results/{model.ResultID}", request);

			if (response.StatusCode == HttpStatusCode.Unauthorized ||
				response.StatusCode == HttpStatusCode.Forbidden)
			{
				return await ApiFailure(response);
			}

			if (!response.IsSuccessStatusCode)
			{
				ModelState.AddModelError("",
					response.StatusCode == HttpStatusCode.Conflict
						? "A result already exists, or the enrolment is cancelled."
						: $"Result could not be saved (HTTP {(int)response.StatusCode}).");

				return View("Form", model);
			}

			TempData["Success"] =
				"Race result saved and available to the participant.";

			return RedirectToAction(nameof(Index),
				new { eventId = model.EventID });
		}
		catch (HttpRequestException)
		{
			ModelState.AddModelError(
				"", "RaceDay API is unavailable.");

			return View("Form", model);
		}
	}
}
