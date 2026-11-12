namespace Zyara.Services.Features.Bookings;

public enum ReviewSubmissionResult
{
    Submitted,
    NotFound,
    NotCompleted,
    AlreadyReviewed,
    InvalidInput
}