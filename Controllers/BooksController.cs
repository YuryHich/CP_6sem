using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryManagement.Services;
using LibraryManagement.DTOs;
using System.Security.Claims;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BookDTO>>> GetBooks(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? genre = null,
        [FromQuery] Guid? author = null)
    {
        try
        {
            var books = await _bookService.GetBooksAsync(page, size, search, genre, author);
            return Ok(books);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookDTO>> GetBook(Guid id)
    {
        try
        {
            var book = await _bookService.GetBookByIdAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            return Ok(book);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("isbn/{isbn}")]
    public async Task<ActionResult<BookDTO>> GetBookByIsbn(string isbn)
    {
        try
        {
            var book = await _bookService.GetBookByIsbnAsync(isbn);
            if (book == null)
            {
                return NotFound();
            }
            return Ok(book);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Guid>> CreateBook([FromBody] BookDTO book)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var bookId = await _bookService.CreateBookAsync(book);
            return CreatedAtAction(nameof(GetBook), new { id = bookId }, new { id = bookId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateBook(Guid id, [FromBody] BookDTO book)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _bookService.UpdateBookAsync(id, book);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteBook(Guid id)
    {
        try
        {
            await _bookService.DeleteBookAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{id}/loan")]
    [Authorize]
    public async Task<IActionResult> LoanBook(Guid id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var success = await _bookService.LoanBookAsync(id, userId);
            if (!success)
            {
                return BadRequest(new { error = "Книга недоступна для займа" });
            }

            return Ok(new { message = "Книга успешно взята" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{id}/return")]
    [Authorize]
    public async Task<IActionResult> ReturnBook(Guid id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var success = await _bookService.ReturnBookAsync(id, userId);
            if (!success)
            {
                return BadRequest(new { error = "Не удалось вернуть книгу" });
            }

            return Ok(new { message = "Книга успешно возвращена" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{id}/image")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Файл не выбран" });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
            {
                return BadRequest(new { error = "Поддерживаются только изображения JPG и PNG" });
            }

            var fileName = $"{id}{extension}";
            var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            Directory.CreateDirectory(imagesPath);
            var filePath = Path.Combine(imagesPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var book = await _bookService.GetBookByIdAsync(id);
            if (book != null)
            {
                book.CoverImagePath = fileName;
                await _bookService.UpdateBookAsync(id, book);
            }

            return Ok(new { imagePath = $"/images/{fileName}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

