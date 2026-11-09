
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels;

public class EventFormViewModel
{
	public int EventID { get; set; }

	[Required]
	[StringLength(150, MinimumLength = 3)]
	[Display(Name = "Event Name")]
	public string EventName { get; set; } = "";

	[Required]
	[StringLength(500, MinimumLength = 5)]
	public string Description { get; set; } = "";

	[Required]
	[Display(Name = "Event Date")]
	public DateTime EventDate { get; set; }
		= DateTime.Today.AddDays(7).AddHours(9);

	[Required]
	[StringLength(150)]
	public string Location { get; set; } = "";

	[Range(
		typeof(decimal),
		"0.1",
		"1000",
		ParseLimitsInInvariantCulture = true,
		ConvertValueInInvariantCulture = true,
		ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
	[Display(Name = "Distance (km)")]
	public decimal Distance { get; set; } = 5;

	[Required]
	[RegularExpression(
		"^(Run|Walk|Cycle)$",
		ErrorMessage = "Event type must be Run, Walk or Cycle.")]
	[Display(Name = "Event Type")]
	public string EventType { get; set; } = "Run";
}
