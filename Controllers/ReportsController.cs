using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryManagement.Services;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpPost("pdf")]
    public async Task<IActionResult> GeneratePdfReport([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        try
        {
            var pdfBytes = await _reportService.GeneratePdfReportAsync(fromDate, toDate);
            return File(pdfBytes, "application/pdf", $"report_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("excel")]
    public async Task<IActionResult> GenerateExcelReport([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        try
        {
            var excelBytes = await _reportService.GenerateExcelReportAsync(fromDate, toDate);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                $"report_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("loans-pdf")]
    public async Task<IActionResult> GenerateLoansPdfReport()
    {
        try
        {
            var pdfBytes = await _reportService.GenerateLoansPdfReportAsync();
            var fileName = $"loans_report_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

