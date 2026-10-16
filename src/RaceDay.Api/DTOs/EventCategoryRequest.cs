using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EventCategoryRequest
{
	[Range(1, int.MaxValue)]
	public int CategoryID { get; set; }

	[Range(typeof(decimal), "0", "99999999.99",
		ParseLimitsInInvariantCulture = true,
		ConvertValueInInvariantCulture = true)]
	public decimal EntryFee { get; set; }

	[Range(1, 100000)]
	public int MaximumParticipants { get; set; }
}
