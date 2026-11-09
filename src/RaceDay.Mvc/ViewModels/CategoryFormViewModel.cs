
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels;

public class CategoryFormViewModel
{
	public int CategoryID { get; set; }

	[Required]
	[StringLength(100, MinimumLength = 2)]
	[Display(Name = "Category Name")]
	public string CategoryName { get; set; } = "";

	[StringLength(255)]
	public string? Description { get; set; }
}
