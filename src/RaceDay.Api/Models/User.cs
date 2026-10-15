
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class User
{
    [Key]
    public int UserID { get; set; }

    [Required, MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = "Participant";

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public ICollection<Event> Events { get; set; } = new List<Event>();

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
