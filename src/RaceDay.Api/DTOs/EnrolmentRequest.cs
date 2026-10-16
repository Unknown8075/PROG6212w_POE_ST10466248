using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EnrolmentRequest
{
	[Range(1, int.MaxValue)]
	public int EventCategoryID { get; set; }
}
