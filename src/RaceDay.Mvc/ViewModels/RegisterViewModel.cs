using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels;

public class RegisterViewModel
{
	[Required]
	public string FirstName { get; set; } = "";

	[Required]
	public string LastName { get; set; } = "";

	[Required]
	[EmailAddress]
	public string Email { get; set; } = "";

	[Required]
	[MinLength(8)]
	[DataType(DataType.Password)]
	public string Password { get; set; } = "";

	[Required]
	public string Role { get; set; } = "Participant";
}
