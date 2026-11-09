
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels;

public class ProfileViewModel
{
	public int UserID { get; set; }

	[Required(ErrorMessage = "First name is required.")]
	[StringLength(50)]
	[Display(Name = "First Name")]
	public string FirstName { get; set; } = string.Empty;

	[Required(ErrorMessage = "Last name is required.")]
	[StringLength(50)]
	[Display(Name = "Last Name")]
	public string LastName { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string Role { get; set; } = string.Empty;

	public DateTime DateCreated { get; set; }
}
