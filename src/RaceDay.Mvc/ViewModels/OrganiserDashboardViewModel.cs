
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.ViewModels;

public class OrganiserDashboardViewModel
{
	public List<RaceEvent> Events { get; set; } = new();

	public int TotalEvents => Events.Count;

	public int UpcomingEvents => Events.Count(
		e => e.EventDate > DateTime.UtcNow);

	public RaceEvent? NextEvent => Events
		.Where(e => e.EventDate > DateTime.UtcNow)
		.OrderBy(e => e.EventDate)
		.FirstOrDefault();
}
