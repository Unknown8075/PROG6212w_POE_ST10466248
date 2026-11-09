
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Participant")]
public class ProfileController : Controller
{
	private readonly RaceDayApiClient _api;

	public ProfileController(RaceDayApiClient api)
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

		TempData["Error"] = "Unable to retrieve your profile.";
		return RedirectToAction("Index", "Participant");
	}

	[HttpGet]
	public async Task<IActionResult> Index()
	{
		try
		{
			using var response = await _api.GetAsync("api/profile");

			if (!response.IsSuccessStatusCode)
				return await ApiFailure(response);

			var model = await response.Content
				.ReadFromJsonAsync<ProfileViewModel>();

			if (model == null)
				return NotFound();

			return View(model);
		}
		catch (HttpRequestException)
		{
			TempData["Error"] = "RaceDay API is unavailable.";
			return RedirectToAction("Index", "Participant");
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Index(ProfileViewModel model)
	{
		if (string.IsNullOrWhiteSpace(model.FirstName))
			ModelState.AddModelError(
				nameof(model.FirstName),
				"First name cannot be empty.");

		if (string.IsNullOrWhiteSpace(model.LastName))
			ModelState.AddModelError(
				nameof(model.LastName),
				"Last name cannot be empty.");

		if (!ModelState.IsValid)
		{
			try
			{
				using var profileResponse =
					await _api.GetAsync("api/profile");

				if (!profileResponse.IsSuccessStatusCode)
					return await ApiFailure(profileResponse);

				var existing = await profileResponse.Content
					.ReadFromJsonAsync<ProfileViewModel>();

				if (existing != null)
				{
					model.Email = existing.Email;
					model.Role = existing.Role;
					model.UserID = existing.UserID;
					model.DateCreated = existing.DateCreated;
				}
			}
			catch (HttpRequestException)
			{
				ModelState.AddModelError(
					"", "Unable to load profile details.");
			}

			return View(model);
		}

		var request = new
		{
			FirstName = model.FirstName.Trim(),
			LastName = model.LastName.Trim()
		};

		try
		{
			using var response = await _api.PutAsync(
				"api/profile", request);

			if (!response.IsSuccessStatusCode)
			{
				if (response.StatusCode == HttpStatusCode.Unauthorized ||
					response.StatusCode == HttpStatusCode.Forbidden)
					return await ApiFailure(response);

				ModelState.AddModelError(
					"",
					"Unable to update your profile.");

				return View(model);
			}

			TempData["Success"] = "Profile updated successfully.";

			return RedirectToAction(nameof(Index));
		}
		catch (HttpRequestException)
		{
			ModelState.AddModelError(
				"", "RaceDay API is unavailable.");

			return View(model);
		}
	}
}
