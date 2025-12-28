namespace PrReviewer.Client.Models;

public class ReviewResponse
{
    public string Status { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int PostedComments { get; set; }
    public int TotalComments { get; set; }
}

public class ApiError
{
    public string Error { get; set; } = string.Empty;
}

