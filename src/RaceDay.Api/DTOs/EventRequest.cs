using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EventRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string EventName { get; set; } = string.Empty;

    [Required, StringLength(500, MinimumLength = 5)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

[Range(
    typeof(decimal),
    "0.1",
    "1000",
    ParseLimitsInInvariantCulture = true,
    ConvertValueInInvariantCulture = true,
    ErrorMessage = "Distance must be between 0.1 and 1000 km."
)]
public decimal Distance { get; set; }

    [Required]
    [RegularExpression(
        "^(Run|Walk|Cycle)$",
        ErrorMessage = "Event type must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = "Run";
}
