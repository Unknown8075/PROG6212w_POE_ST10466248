
using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Enrolment
{
	[Key]
	public int EnrolmentID { get; set; }

	[Required]
	public int ParticipantID { get; set; }

	[Required]
	public int EventCategoryID { get; set; }

	public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

	[Required, MaxLength(20)]
	public string Status { get; set; } = "Pending";

	public User Participant { get; set; } = null!;

	public EventCategory EventCategory { get; set; } = null!;

	public Result? Result { get; set; }
}
