using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LanguagesController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public LanguagesController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet]
    public async Task<ActionResult<List<LanguageDTO>>> GetLanguages()
    {
        try
        {
            var languages = await _lookupService.GetLanguagesAsync();
            return Ok(languages);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<LanguageDTO>> CreateLanguage([FromBody] LanguageDTO language)
    {
        if (language == null || string.IsNullOrWhiteSpace(language.LanguageName))
        {
            return BadRequest(new { error = "Название языка обязательно" });
        }

        try
        {
            var created = await _lookupService.CreateLanguageAsync(language);
            return CreatedAtAction(nameof(GetLanguages), new { id = created.LanguageId }, created);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            return Conflict(new { error = "Язык с таким названием уже существует" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateLanguage(Guid id, [FromBody] LanguageDTO language)
    {
        if (language == null || string.IsNullOrWhiteSpace(language.LanguageName))
        {
            return BadRequest(new { error = "Название языка обязательно" });
        }

        try
        {
            await _lookupService.UpdateLanguageAsync(id, language);
            return NoContent();
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            return Conflict(new { error = "Язык с таким названием уже существует" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteLanguage(Guid id)
    {
        try
        {
            var success = await _lookupService.DeleteLanguageAsync(id);
            if (!success)
            {
                return BadRequest(new { error = "Нельзя удалить язык, который используется книгами" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}


