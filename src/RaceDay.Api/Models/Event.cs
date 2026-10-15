using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class Event
{
	[Key]
	public int EventID { get; set; }

	[Required]
	public int OrganiserID { get; set; }

	[Required, MaxLength(150)]
	public string EventName { get; set; } = string.Empty;

	[Required, MaxLength(500)]
	public string Description { get; set; } = string.Empty;

	[Required]
	public DateTime EventDate { get; set; }

	[Required, MaxLength(150)]
	public string Location { get; set; } = string.Empty;

	[Range(0.1, 1000)]
	[Column(TypeName = "decimal(10,2)")]
	public decimal Distance { get; set; }

	[Required, MaxLength(20)]
	public string EventType { get; set; } = "Run";

	public DateTime DateCreated { get; set; } = DateTime.UtcNow;

	[ForeignKey(nameof(OrganiserID))]
	public User Organiser { get; set; } = null!;

	public ICollection<EventCategory> EventCategories { get; set; }
		= new List<EventCategory>();
}
