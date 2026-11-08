using Microsoft.AspNetCore.Authentication.Cookies;
using RaceDay.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
	options.Cookie.Name = "RaceDay.Mvc.Session";
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
	options.Cookie.SameSite = SameSiteMode.Lax;
	options.IdleTimeout = TimeSpan.FromHours(2);
});

builder.Services
	.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.Cookie.Name = "RaceDay.Mvc.Auth";
		options.Cookie.HttpOnly = true;
		options.Cookie.SameSite = SameSiteMode.Lax;
		options.ExpireTimeSpan = TimeSpan.FromHours(2);
		options.SlidingExpiration = false;
		options.LoginPath = "/Account/Login";
		options.AccessDeniedPath = "/Account/AccessDenied";
	});

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<RaceDayApiClient>(client =>
{
	client.BaseAddress = new Uri(
		builder.Configuration["RaceDayApi:BaseUrl"]
		?? "https://localhost:7020/");

}).ConfigurePrimaryHttpMessageHandler(() =>
	new HttpClientHandler
	{
		UseCookies = false
	});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
