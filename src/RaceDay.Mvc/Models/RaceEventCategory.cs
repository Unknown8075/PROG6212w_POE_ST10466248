
namespace RaceDay.Mvc.Models;

public class RaceEventCategory
{
	public int EventCategoryID { get; set; }
	public int EventID { get; set; }
	public int CategoryID { get; set; }
	public string CategoryName { get; set; } = "";
	public decimal EntryFee { get; set; }
	public int MaximumParticipants { get; set; }
}
