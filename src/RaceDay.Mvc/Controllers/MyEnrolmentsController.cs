using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Participant")]
public class MyEnrolmentsController : Controller
{
	private readonly RaceDayApiClient _api;

	public MyEnrolmentsController(RaceDayApiClient api)
	{
		_api = api;
	}

	public async Task<IActionResult> Index()
	{
		try
		{
			using var response =
				await _api.GetAsync("api/enrolments/my");

			if (response.StatusCode == HttpStatusCode.Unauthorized)
				return RedirectToAction("Login", "Account");

			if (!response.IsSuccessStatusCode)
			{
				TempData["Error"] = "Unable to load enrolments.";
				return View(new List<ParticipantEnrolment>());
			}

			var enrolments = await response.Content
				.ReadFromJsonAsync<List<ParticipantEnrolment>>();

			return View(enrolments ?? new());
		}
		catch (HttpRequestException)
		{
			TempData["Error"] = "RaceDay API is unavailable.";
			return View(new List<ParticipantEnrolment>());
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Enrol(int eventCategoryId)
	{
		if (eventCategoryId <= 0)
			return BadRequest();

		try
		{
			using var response = await _api.PostAsync(
				"api/enrolments",
				new { EventCategoryID = eventCategoryId });

			if (response.StatusCode == HttpStatusCode.Unauthorized)
				return RedirectToAction("Login", "Account");

			TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
				response.IsSuccessStatusCode
					? "Successfully enrolled! Status: Pending."
					: await ReadError(response);
		}
		catch (HttpRequestException)
		{
			TempData["Error"] = "RaceDay API is unavailable.";
		}

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Cancel(int id)
	{
		if (id <= 0)
			return BadRequest();

		try
		{
			using var request = new HttpRequestMessage(
				HttpMethod.Put, $"api/enrolments/{id}/cancel");

			using var response = await _api.SendAsync(request);

			if (response.StatusCode == HttpStatusCode.Unauthorized)
				return RedirectToAction("Login", "Account");

			TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
				response.IsSuccessStatusCode
					? "Enrolment cancelled successfully."
					: await ReadError(response);
		}
		catch (HttpRequestException)
		{
			TempData["Error"] = "RaceDay API is unavailable.";
		}

		return RedirectToAction(nameof(Index));
	}

	public async Task<IActionResult> Results()
	{
		try
		{
			using var response =
				await _api.GetAsync("api/results/my");

			if (response.StatusCode == HttpStatusCode.Unauthorized)
				return RedirectToAction("Login", "Account");

			if (!response.IsSuccessStatusCode)
			{
				TempData["Error"] = "Unable to load race results.";
				return View(new List<ParticipantRaceResult>());
			}

			var results = await response.Content
				.ReadFromJsonAsync<List<ParticipantRaceResult>>();

			return View(results ?? new());
		}
		catch (HttpRequestException)
		{
			TempData["Error"] = "RaceDay API is unavailable.";
			return View(new List<ParticipantRaceResult>());
		}
	}

	private static async Task<string> ReadError(
		HttpResponseMessage response)
	{
		try
		{
			using var document = System.Text.Json.JsonDocument.Parse(
				await response.Content.ReadAsStringAsync());

			if (document.RootElement.TryGetProperty(
				"message", out var message))
				return message.GetString() ?? "Request failed.";
		}
		catch (System.Text.Json.JsonException)
		{
		}

		return $"Request failed ({(int)response.StatusCode}).";
	}
}
