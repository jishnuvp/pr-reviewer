using System.ComponentModel.DataAnnotations;

namespace PrReviewer.Client.Models;

public class ReviewRequest
{
    [Required(ErrorMessage = "PR URL is required")]
    [Url(ErrorMessage = "Please enter a valid URL")]
    public string PrUrl { get; set; } = string.Empty;
    
    public string? AdditionalInformation { get; set; }
}

