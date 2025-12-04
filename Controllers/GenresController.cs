using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

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
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            return Conflict(new { error = "Жанр с таким названием уже существует" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateGenre(Guid id, [FromBody] GenreDTO genre)
    {
        if (genre == null || string.IsNullOrWhiteSpace(genre.GenreName))
        {
            return BadRequest(new { error = "Название жанра обязательно" });
        }

        try
        {
            await _lookupService.UpdateGenreAsync(id, genre);
            return NoContent();
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            return Conflict(new { error = "Жанр с таким названием уже существует" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteGenre(Guid id)
    {
        try
        {
            var success = await _lookupService.DeleteGenreAsync(id);
            if (!success)
            {
                return BadRequest(new { error = "Нельзя удалить жанр, который используется книгами" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}


