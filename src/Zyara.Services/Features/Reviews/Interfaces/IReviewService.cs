using Zyara.Services.Common;
using Zyara.Services.Features.Reviews.Dtos;

namespace Zyara.Services.Features.Reviews.Interfaces;

/// <summary>
/// Reviews after a visit. One review per completed booking; the provider's AverageRating / RatingCount
/// are updated in the same transaction. patientId always comes from ICurrentUser, never from a form or URL.
/// </summary>
public interface IReviewService
{
    /// <summary>"My reviews": completed visits still waiting for a review + reviews already written.</summary>
    Task<PatientReviewsPageDto> GetPageAsync(int patientId, CancellationToken cancellationToken = default);

    /// <summary>Data for the "Rate your visit" page. Fails when the visit is not the patient's, not completed, or already reviewed.</summary>
    Task<ServiceResult<ReviewFormDto>> GetFormAsync(int patientId, int bookingId, CancellationToken cancellationToken = default);

    Task<ServiceResult> SubmitAsync(int patientId, CreateReviewDto dto, CancellationToken cancellationToken = default);
}
