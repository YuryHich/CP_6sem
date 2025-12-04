using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

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
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            return Conflict(new { error = "Издательство с таким названием уже существует" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdatePublisher(Guid id, [FromBody] PublisherDTO publisher)
    {
        if (publisher == null || string.IsNullOrWhiteSpace(publisher.PublisherName))
        {
            return BadRequest(new { error = "Название издательства обязательно" });
        }

        try
        {
            await _lookupService.UpdatePublisherAsync(id, publisher);
            return NoContent();
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            return Conflict(new { error = "Издательство с таким названием уже существует" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeletePublisher(Guid id)
    {
        try
        {
            var success = await _lookupService.DeletePublisherAsync(id);
            if (!success)
            {
                return BadRequest(new { error = "Нельзя удалить издательство, которое используется книгами" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}


