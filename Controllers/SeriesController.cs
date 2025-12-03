using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeriesController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public SeriesController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet]
    public async Task<ActionResult<List<SeriesDTO>>> GetSeries()
    {
        try
        {
            var series = await _lookupService.GetSeriesAsync();
            return Ok(series);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<SeriesDTO>> CreateSeries([FromBody] SeriesDTO series)
    {
        if (series == null || string.IsNullOrWhiteSpace(series.SeriesName))
        {
            return BadRequest(new { error = "Название серии обязательно" });
        }

        try
        {
            var created = await _lookupService.CreateSeriesAsync(series);
            return CreatedAtAction(nameof(GetSeries), new { id = created.SeriesId }, created);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}


