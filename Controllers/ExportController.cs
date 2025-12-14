using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryManagement.Services;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;

    public ExportController(IExportService exportService)
    {
        _exportService = exportService;
    }

    [HttpPost("csv")]
    public async Task<IActionResult> ExportToCsv()
    {
        try
        {
            var csvBytes = await _exportService.ExportDatabaseToCsvAsync();
            var fileName = $"library_backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
            
            return File(csvBytes, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

