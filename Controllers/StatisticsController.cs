using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryManagement.Services;
using System.Security.Claims;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet("global")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetGlobalStatistics()
    {
        try
        {
            var stats = await _statisticsService.GetGlobalStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserStatistics()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Пользователь не авторизован" });
            }

            var stats = await _statisticsService.GetUserStatisticsAsync(userId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("authors")]
    public async Task<IActionResult> GetAuthorStatistics()
    {
        try
        {
            var stats = await _statisticsService.GetAuthorStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("genres")]
    public async Task<IActionResult> GetGenreStatistics()
    {
        try
        {
            var stats = await _statisticsService.GetGenreStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("languages")]
    public async Task<IActionResult> GetLanguageStatistics()
    {
        try
        {
            var stats = await _statisticsService.GetLanguageStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

