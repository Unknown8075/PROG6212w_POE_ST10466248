
using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Result
{
	[Key]
	public int ResultID { get; set; }

	[Required]
	public int EnrolmentID { get; set; }

	public TimeSpan? FinishTime { get; set; }

	public int? Position { get; set; }

	[Required, MaxLength(20)]
	public string ResultStatus { get; set; } = "Finished";

	public Enrolment Enrolment { get; set; } = null!;
}
