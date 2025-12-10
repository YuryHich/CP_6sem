using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public interface IReviewService
{
    Task<List<ReviewDTO>> GetReviewsByBookAsync(Guid bookId);
    Task<ReviewDTO> CreateReviewAsync(Guid bookId, Guid userId, int rating, string? comment);
    Task DeleteReviewAsync(Guid reviewId);
}




