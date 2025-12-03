using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryManagement.Services;
using LibraryManagement.DTOs;
using System.Security.Claims;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}/loans")]
    public async Task<ActionResult<List<LoanDTO>>> GetUserLoans(Guid id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            // Пользователь может видеть только свои займы, админ - любые
            var isAdmin = User.IsInRole("admin");
            if (!isAdmin && userId != id)
            {
                return Forbid();
            }

            var loans = await _userService.GetUserLoansAsync(id);
            return Ok(loans);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

