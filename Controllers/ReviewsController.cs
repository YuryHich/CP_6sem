using System.Security.Claims;
using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet("book/{bookId}")]
    public async Task<ActionResult<List<ReviewDTO>>> GetBookReviews(Guid bookId)
    {
        try
        {
            var reviews = await _reviewService.GetReviewsByBookAsync(bookId);
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    public class CreateReviewRequest
    {
        public Guid BookId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReviewDTO>> CreateReview([FromBody] CreateReviewRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            if (request.BookId == Guid.Empty)
            {
                return BadRequest(new { error = "Не указан идентификатор книги" });
            }

            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest(new { error = "Оценка должна быть от 1 до 5" });
            }

            var review = await _reviewService.CreateReviewAsync(request.BookId, userId, request.Rating, request.Comment);
            return Ok(review);
        }
        catch (InvalidOperationException ex)
        {
            // Пользователь уже оставлял отзыв
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        try
        {
            await _reviewService.DeleteReviewAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}


