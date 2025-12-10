using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class HistoryController : ControllerBase
{
    private readonly IHistoryService _historyService;

    public HistoryController(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    private static IReadOnlyCollection<string> ParseOperations(string? operations)
    {
        if (string.IsNullOrWhiteSpace(operations))
        {
            return Array.Empty<string>();
        }

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Insert", "Update", "Delete" };
        return operations
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(op => allowed.Contains(op))
            .Select(op => char.ToUpperInvariant(op[0]) + op[1..].ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    [HttpGet("books")]
    public async Task<ActionResult<List<HistoryRecordDTO>>> GetBooksHistory(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? operations,
        [FromQuery] string? username)
    {
        try
        {
            var ops = ParseOperations(operations);
            var records = await _historyService.GetBooksHistoryAsync(fromDate, toDate, ops, username);
            return Ok(records);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("loans")]
    public async Task<ActionResult<List<HistoryRecordDTO>>> GetLoansHistory(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? operations,
        [FromQuery] string? username)
    {
        try
        {
            var ops = ParseOperations(operations);
            var records = await _historyService.GetLoansHistoryAsync(fromDate, toDate, ops, username);
            return Ok(records);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("usernames")]
    public async Task<ActionResult<List<string>>> GetUsernames()
    {
        try
        {
            var usernames = await _historyService.GetUsernamesAsync();
            return Ok(usernames);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}




