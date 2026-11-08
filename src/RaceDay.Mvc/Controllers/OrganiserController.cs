using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Organiser")]
public class OrganiserController : Controller
{
	public IActionResult Index()
	{
		return View();
	}
}
