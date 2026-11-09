namespace RaceDay.Mvc.Models;

public class ParticipantEnrolment
{
	public int EnrolmentID { get; set; }
	public int EventCategoryID { get; set; }
	public int EventID { get; set; }
	public string EventName { get; set; } = "";
	public string CategoryName { get; set; } = "";
	public DateTime EnrolmentDate { get; set; }
	public string Status { get; set; } = "";
}
