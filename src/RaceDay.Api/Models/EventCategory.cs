
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class EventCategory
{
	[Key]
	public int EventCategoryID { get; set; }

	[Required]
	public int EventID { get; set; }

	[Required]
	public int CategoryID { get; set; }

	[Column(TypeName = "decimal(10,2)")]
	public decimal EntryFee { get; set; }

	public int MaximumParticipants { get; set; }

	public Event Event { get; set; } = null!;

	public Category Category { get; set; } = null!;

	public ICollection<Enrolment> Enrolments { get; set; }
		= new List<Enrolment>();
}
