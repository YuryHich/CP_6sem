using LibraryManagement.DataAccess;
using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public class ReviewService : IReviewService
{
    private readonly ReviewRepository _repository;

    public ReviewService(DatabaseConnection db)
    {
        _repository = new ReviewRepository(db);
    }

    public Task<List<ReviewDTO>> GetReviewsByBookAsync(Guid bookId)
    {
        return _repository.GetReviewsByBookAsync(bookId);
    }

    public async Task<ReviewDTO> CreateReviewAsync(Guid bookId, Guid userId, int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Оценка должна быть от 1 до 5");
        }

        var alreadyExists = await _repository.UserHasReviewAsync(bookId, userId);
        if (alreadyExists)
        {
            throw new InvalidOperationException("Вы уже оставляли отзыв на эту книгу");
        }

        return await _repository.CreateReviewAsync(bookId, userId, rating, comment);
    }

    public Task DeleteReviewAsync(Guid reviewId)
    {
        return _repository.DeleteReviewAsync(reviewId);
    }
}




