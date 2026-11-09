
namespace RaceDay.Mvc.ViewModels;

public class OrganiserEnrolmentViewModel
{
	public int EnrolmentID { get; set; }
	public int ParticipantID { get; set; }
	public int EventCategoryID { get; set; }
	public int EventID { get; set; }
	public string CategoryName { get; set; } = "";
	public DateTime EnrolmentDate { get; set; }
	public string Status { get; set; } = "";
}
