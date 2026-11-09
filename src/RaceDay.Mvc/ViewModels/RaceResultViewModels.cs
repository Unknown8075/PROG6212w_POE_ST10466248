
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels;

public class OrganiserRaceResult
{
	public int ResultID { get; set; }
	public int EnrolmentID { get; set; }
	public int ParticipantID { get; set; }
	public TimeSpan? FinishTime { get; set; }
	public int? Position { get; set; }
	public string ResultStatus { get; set; } = "";
}

public class RaceResultFormViewModel
{
	public int EventID { get; set; }
	public int ResultID { get; set; }

	[Range(1, int.MaxValue)]
	public int EnrolmentID { get; set; }

	[Display(Name = "Finish Time (HH:mm:ss)")]
	public string? FinishTimeText { get; set; }

	[Range(1, int.MaxValue)]
	public int? Position { get; set; }

	[Required]
	[RegularExpression("^(Finished|DNF|DNS|DQ)$")]
	[Display(Name = "Result Status")]
	public string ResultStatus { get; set; } = "Finished";
}
