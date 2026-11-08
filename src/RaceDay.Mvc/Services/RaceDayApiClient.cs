using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Mvc.Services;

public sealed class RaceDayApiClient
{
	private readonly HttpClient _http;
	private readonly IHttpContextAccessor _accessor;

	public RaceDayApiClient(
		HttpClient http,
		IHttpContextAccessor accessor)
	{
		_http = http;
		_accessor = accessor;
	}

	public async Task<HttpResponseMessage> PostAsync<T>(
		string endpoint, T data)
	{
		using var request = new HttpRequestMessage(
			HttpMethod.Post, endpoint);

		request.Content = JsonContent.Create(data);

		return await SendAsync(request);
	}

	public async Task<HttpResponseMessage> GetAsync(
		string endpoint)
	{
		using var request = new HttpRequestMessage(
			HttpMethod.Get, endpoint);

		return await SendAsync(request);
	}

	public async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request)
	{
		var cookie = _accessor.HttpContext?
			.Session.GetString("ApiAuthCookie");

		if (!string.IsNullOrWhiteSpace(cookie))
		{
			request.Headers.TryAddWithoutValidation(
				"Cookie", cookie);
		}

		return await _http.SendAsync(request);
	}

	public async Task<bool> ValidateSessionAsync()
	{
		using var response = await GetAsync("api/auth/me");
		return response.StatusCode == HttpStatusCode.OK;
	}

	public static string? ExtractAuthCookie(
		HttpResponseMessage response)
	{
		if (!response.Headers.TryGetValues(
			"Set-Cookie", out var values))
			return null;

		foreach (var value in values)
		{
			var firstPart = value.Split(';', 2)[0];

			if (firstPart.StartsWith(
				"RaceDay.Auth=", StringComparison.Ordinal))
				return firstPart;
		}

		return null;
	}
}
