
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Organiser")]
public class EventCategoriesController : Controller
{
	private readonly RaceDayApiClient _api;

	public EventCategoriesController(RaceDayApiClient api)
	{
		_api = api;
	}

	private int CurrentUserId =>
		int.TryParse(
			User.FindFirstValue(ClaimTypes.NameIdentifier),
			out var id) ? id : -1;

	private async Task<bool> OwnsEvent(int eventId)
	{
		using var response =
			await _api.GetAsync($"api/events/{eventId}");

		if (!response.IsSuccessStatusCode)
			return false;

		var raceEvent = await response.Content
			.ReadFromJsonAsync<RaceEvent>();

		return raceEvent?.OrganiserID == CurrentUserId;
	}

	private async Task LoadCategories(EventCategoryFormViewModel model)
	{
		using var response = await _api.GetAsync("api/categories");
		response.EnsureSuccessStatusCode();

		var categories = await response.Content
			.ReadFromJsonAsync<List<RaceCategory>>() ?? new();

		model.Categories = categories.Select(c => new SelectListItem
		{
			Value = c.CategoryID.ToString(),
			Text = c.CategoryName
		}).ToList();
	}

	[HttpGet]
	public async Task<IActionResult> Index(int eventId)
	{
		if (!await OwnsEvent(eventId))
			return Forbid();

		using var response =
			await _api.GetAsync($"api/events/{eventId}/categories");

		if (!response.IsSuccessStatusCode)
			return StatusCode((int)response.StatusCode);

		var categories = await response.Content
			.ReadFromJsonAsync<List<RaceEventCategory>>() ?? new();

		ViewBag.EventID = eventId;
		return View(categories);
	}

	[HttpGet]
	public async Task<IActionResult> Create(int eventId)
	{
		if (!await OwnsEvent(eventId))
			return Forbid();

		var model = new EventCategoryFormViewModel
		{
			EventID = eventId
		};

		await LoadCategories(model);
		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(
		EventCategoryFormViewModel model)
	{
		if (!await OwnsEvent(model.EventID))
			return Forbid();

		if (!ModelState.IsValid)
		{
			await LoadCategories(model);
			return View(model);
		}

		using var response = await _api.PostAsync(
			$"api/events/{model.EventID}/categories",
			new
			{
				model.CategoryID,
				model.EntryFee,
				model.MaximumParticipants
			});

		if (response.StatusCode == HttpStatusCode.Conflict)
		{
			ModelState.AddModelError("",
				"This category is already assigned to the event.");
			await LoadCategories(model);
			return View(model);
		}

		if (!response.IsSuccessStatusCode)
		{
			ModelState.AddModelError("",
				$"Unable to assign category ({(int)response.StatusCode}).");
			await LoadCategories(model);
			return View(model);
		}

		TempData["Success"] = "Category assigned to event.";

		return RedirectToAction(nameof(Index),
			new { eventId = model.EventID });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(
		int eventId, int eventCategoryId)
	{
		if (!await OwnsEvent(eventId))
			return Forbid();

		using var response = await _api.DeleteAsync(
			$"api/events/{eventId}/categories/{eventCategoryId}");

		TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
			response.IsSuccessStatusCode
				? "Category removed."
				: "Unable to remove category. Check existing enrolments.";

		return RedirectToAction(nameof(Index), new { eventId });
	}
}
