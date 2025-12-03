using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}


