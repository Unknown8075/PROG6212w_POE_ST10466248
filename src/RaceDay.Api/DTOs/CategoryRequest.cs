using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CategoryRequest
{
	[Required(ErrorMessage = "Category name is required.")]
	[StringLength(100, MinimumLength = 2)]
	public string CategoryName { get; set; } = string.Empty;

	[StringLength(255)]
	public string? Description { get; set; }
}
