
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RaceDay.Mvc.ViewModels;

public class EventCategoryFormViewModel
{
	public int EventID { get; set; }

	[Range(1, int.MaxValue, ErrorMessage = "Select a category.")]
	public int CategoryID { get; set; }

	[Range(
		typeof(decimal), "0", "99999999.99",
		ParseLimitsInInvariantCulture = true,
		ConvertValueInInvariantCulture = true)]
	[Display(Name = "Entry Fee (R)")]
	public decimal EntryFee { get; set; }

	[Range(1, 100000)]
	[Display(Name = "Maximum Participants")]
	public int MaximumParticipants { get; set; } = 100;

	public List<SelectListItem> Categories { get; set; } = new();
}
