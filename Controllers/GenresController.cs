using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenresController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public GenresController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet]
    public async Task<ActionResult<List<GenreDTO>>> GetGenres()
    {
        try
        {
            var genres = await _lookupService.GetGenresAsync();
            return Ok(genres);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<GenreDTO>> CreateGenre([FromBody] GenreDTO genre)
    {
        if (genre == null || string.IsNullOrWhiteSpace(genre.GenreName))
        {
            return BadRequest(new { error = "Название жанра обязательно" });
        }

        try
        {
            var created = await _lookupService.CreateGenreAsync(genre);
            return CreatedAtAction(nameof(GetGenres), new { id = created.GenreId }, created);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}


