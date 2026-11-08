using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public class AccountController : Controller
{
	private readonly RaceDayApiClient _api;

	public AccountController(RaceDayApiClient api)
	{
		_api = api;
	}

	[HttpGet]
	[AllowAnonymous]
	public IActionResult Login()
	{
		return View(new LoginViewModel());
	}

	[HttpPost]
	[AllowAnonymous]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Login(
		LoginViewModel model)
	{
		if (!ModelState.IsValid)
			return View(model);

		try
		{
			using var response = await _api.PostAsync(
				"api/auth/login", model);

			if (!response.IsSuccessStatusCode)
			{
				ModelState.AddModelError(
					"", "Invalid email or password.");
				return View(model);
			}

			var user = await response.Content
				.ReadFromJsonAsync<ApiLoginResponse>();

			var apiCookie =
				RaceDayApiClient.ExtractAuthCookie(response);

			if (user == null ||
				string.IsNullOrWhiteSpace(apiCookie) ||
				(user.Role != "Organiser" &&
				 user.Role != "Participant"))
			{
				ModelState.AddModelError(
					"", "Unable to establish your session.");
				return View(model);
			}

			HttpContext.Session.Clear();
			HttpContext.Session.SetString(
				"ApiAuthCookie", apiCookie);

			var claims = new List<Claim>
			{
				new(ClaimTypes.NameIdentifier,
					user.UserID.ToString()),
				new(ClaimTypes.Name, user.Email),
				new(ClaimTypes.Email, user.Email),
				new(ClaimTypes.Role, user.Role)
			};

			var identity = new ClaimsIdentity(
				claims,
				CookieAuthenticationDefaults.AuthenticationScheme);

			await HttpContext.SignInAsync(
				CookieAuthenticationDefaults.AuthenticationScheme,
				new ClaimsPrincipal(identity));

			return user.Role == "Organiser"
				? RedirectToAction("Index", "Organiser")
				: RedirectToAction("Index", "Participant");
		}
		catch (HttpRequestException)
		{
			ModelState.AddModelError(
				"", "The RaceDay service is unavailable.");
			return View(model);
		}
	}

	[HttpGet]
	[AllowAnonymous]
	public IActionResult Register()
	{
		return View(new RegisterViewModel());
	}

	[HttpPost]
	[AllowAnonymous]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Register(
		RegisterViewModel model)
	{
		if (!ModelState.IsValid)
			return View(model);

		if (model.Role != "Organiser" &&
			model.Role != "Participant")
		{
			ModelState.AddModelError(
				"Role", "Please select a valid role.");
			return View(model);
		}

		try
		{
			using var response = await _api.PostAsync(
				"api/auth/register", model);

			if (response.StatusCode == HttpStatusCode.Conflict)
			{
				ModelState.AddModelError(
					"Email", "Email already registered.");
				return View(model);
			}

			if (!response.IsSuccessStatusCode)
			{
				ModelState.AddModelError(
					"", "Please check your registration details.");
				return View(model);
			}

			TempData["Success"] =
				"Registration successful. Please log in.";

			return RedirectToAction(nameof(Login));
		}
		catch (HttpRequestException)
		{
			ModelState.AddModelError(
				"", "The RaceDay service is unavailable.");
			return View(model);
		}
	}

	[Authorize]
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Logout()
	{
		try
		{
			using var response = await _api.PostAsync(
				"api/auth/logout", new { });
		}
		catch (HttpRequestException)
		{
			// Clear the local session even if the API is unavailable.
		}

		HttpContext.Session.Clear();

		await HttpContext.SignOutAsync(
			CookieAuthenticationDefaults.AuthenticationScheme);

		return RedirectToAction("Index", "Home");
	}

	[HttpGet]
	public IActionResult AccessDenied()
	{
		return View();
	}

	private sealed class ApiLoginResponse
	{
		public int UserID { get; set; }
		public string Email { get; set; } = "";
		public string Role { get; set; } = "";
	}
}
