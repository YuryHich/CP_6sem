using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PublishersController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public PublishersController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PublisherDTO>>> GetPublishers()
    {
        try
        {
            var publishers = await _lookupService.GetPublishersAsync();
            return Ok(publishers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<PublisherDTO>> CreatePublisher([FromBody] PublisherDTO publisher)
    {
        if (publisher == null || string.IsNullOrWhiteSpace(publisher.PublisherName))
        {
            return BadRequest(new { error = "Название издательства обязательно" });
        }

        try
        {
            var created = await _lookupService.CreatePublisherAsync(publisher);
            return CreatedAtAction(nameof(GetPublishers), new { id = created.PublisherId }, created);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}


