using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryManagement.Services;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class ImportController : ControllerBase
{
    private readonly IImportService _importService;

    public ImportController(IImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("csv")]
    public async Task<IActionResult> ImportFromCsv(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "Файл не выбран" });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Поддерживаются только файлы .csv" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var (success, message) = await _importService.ImportFromCsvAsync(stream);
            
            if (success)
            {
                return Ok(new { message });
            }
            else
            {
                return BadRequest(new { error = message });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("xml")]
    public async Task<IActionResult> ImportFromXml(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "Файл не выбран" });
        }

        if (!file.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Поддерживаются только файлы .xml" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var (success, message) = await _importService.ImportFromXmlAsync(stream);
            
            if (success)
            {
                return Ok(new { message });
            }
            else
            {
                return BadRequest(new { error = message });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

