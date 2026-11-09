
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Organiser")]
public class CategoriesController : Controller
{
	private readonly RaceDayApiClient _api;

	public CategoriesController(RaceDayApiClient api)
	{
		_api = api;
	}

	private async Task<IActionResult> Failed(HttpStatusCode status)
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

		TempData["Error"] = $"Category request failed: {(int)status}";
		return RedirectToAction(nameof(Index));
	}

	[HttpGet]
	public async Task<IActionResult> Index()
	{
		try
		{
			using var response = await _api.GetAsync("api/categories");

			if (!response.IsSuccessStatusCode)
				return await Failed(response.StatusCode);

			var categories = await response.Content
				.ReadFromJsonAsync<List<RaceCategory>>()
				?? new();

			return View(categories);
		}
		catch (HttpRequestException)
		{
			TempData["Error"] = "Unable to connect to the RaceDay API.";
			return View(new List<RaceCategory>());
		}
	}

	[HttpGet]
	public IActionResult Create() => View(new CategoryFormViewModel());

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(CategoryFormViewModel model)
	{
		if (!ModelState.IsValid)
			return View(model);

		try
		{
			using var response =
				await _api.PostAsync("api/categories", model);

			if (response.StatusCode == HttpStatusCode.Conflict)
			{
				ModelState.AddModelError(
					nameof(model.CategoryName),
					"This category already exists.");
				return View(model);
			}

			if (response.StatusCode == HttpStatusCode.BadRequest)
			{
				ModelState.AddModelError("", "Invalid category details.");
				return View(model);
			}

			if (!response.IsSuccessStatusCode)
				return await Failed(response.StatusCode);

			TempData["Success"] = "Category created successfully.";
			return RedirectToAction(nameof(Index));
		}
		catch (HttpRequestException)
		{
			ModelState.AddModelError("", "RaceDay API is unavailable.");
			return View(model);
		}
	}

	[HttpGet]
	public async Task<IActionResult> Edit(int id)
	{
		using var response =
			await _api.GetAsync($"api/categories/{id}");

		if (response.StatusCode == HttpStatusCode.NotFound)
			return NotFound();

		if (!response.IsSuccessStatusCode)
			return await Failed(response.StatusCode);

		var category = await response.Content
			.ReadFromJsonAsync<RaceCategory>();

		if (category == null)
			return NotFound();

		return View(new CategoryFormViewModel
		{
			CategoryID = category.CategoryID,
			CategoryName = category.CategoryName,
			Description = category.Description
		});
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(
		int id, CategoryFormViewModel model)
	{
		if (id != model.CategoryID)
			return BadRequest();

		if (!ModelState.IsValid)
			return View(model);

		using var response =
			await _api.PutAsync($"api/categories/{id}", model);

		if (response.StatusCode == HttpStatusCode.Conflict ||
			response.StatusCode == HttpStatusCode.BadRequest)
		{
			ModelState.AddModelError("",
				"Invalid or duplicate category name.");
			return View(model);
		}

		if (!response.IsSuccessStatusCode)
			return await Failed(response.StatusCode);

		TempData["Success"] = "Category updated successfully.";
		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(int id)
	{
		using var response =
			await _api.DeleteAsync($"api/categories/{id}");

		if (response.StatusCode == HttpStatusCode.Conflict)
		{
			TempData["Error"] =
				"Cannot delete a category assigned to an event.";
			return RedirectToAction(nameof(Index));
		}

		if (!response.IsSuccessStatusCode)
			return await Failed(response.StatusCode);

		TempData["Success"] = "Category deleted successfully.";
		return RedirectToAction(nameof(Index));
	}
}
