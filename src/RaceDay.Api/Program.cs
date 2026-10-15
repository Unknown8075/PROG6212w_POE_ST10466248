
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure SQL Server database
var connectionString = builder.Configuration
	.GetConnectionString("RaceDayConnection");

builder.Services.AddDbContext<RaceDayDbContext>(options =>
	options.UseSqlServer(connectionString));

// 2. Register API controllers
builder.Services.AddControllers();

// 3. Configure secure password hashing
builder.Services.AddScoped<
	IPasswordHasher<User>,
	PasswordHasher<User>>();

// 4. Configure cookie-based authentication
builder.Services
	.AddAuthentication(
		CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.Cookie.Name = "RaceDay.Auth";
		options.Cookie.HttpOnly = true;
		options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
		options.Cookie.SameSite = SameSiteMode.Strict;

		options.ExpireTimeSpan = TimeSpan.FromHours(2);
		options.SlidingExpiration = true;

		// Return HTTP status codes instead of redirects
		options.Events.OnRedirectToLogin = context =>
		{
			context.Response.StatusCode = 401;
			return Task.CompletedTask;
		};

		options.Events.OnRedirectToAccessDenied = context =>
		{
			context.Response.StatusCode = 403;
			return Task.CompletedTask;
		};
	});

// 5. Configure role-based authorisation
builder.Services.AddAuthorization();

// 6. Configure OpenAPI and Swagger
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 7. Enable API documentation in development
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.UseSwagger();
	app.UseSwaggerUI();
}

// 8. Configure middleware
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// 9. Map controller endpoints
app.MapControllers();

app.Run();
