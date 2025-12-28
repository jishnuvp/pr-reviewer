using PrReviewer.Client.Models;

namespace PrReviewer.Client.Services;

public interface IPrReviewService
{
    Task<ReviewResponse?> SubmitReviewRequestAsync(ReviewRequest request);
}

