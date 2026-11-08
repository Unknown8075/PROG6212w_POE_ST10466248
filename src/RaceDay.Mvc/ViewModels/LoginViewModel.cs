using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels;

public class LoginViewModel
{
	[Required]
	[EmailAddress]
	public string Email { get; set; } = "";

	[Required]
	[DataType(DataType.Password)]
	public string Password { get; set; } = "";
}
