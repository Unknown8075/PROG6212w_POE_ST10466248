using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class ResultRequest
{
	[Range(1, int.MaxValue)]
	public int EnrolmentID { get; set; }

	public TimeSpan? FinishTime { get; set; }

	[Range(1, int.MaxValue)]
	public int? Position { get; set; }

	[Required]
	[RegularExpression("^(Finished|DNF|DNS|DQ)$",
		ErrorMessage = "Status must be Finished, DNF, DNS or DQ.")]
	public string ResultStatus { get; set; } = "Finished";
}
