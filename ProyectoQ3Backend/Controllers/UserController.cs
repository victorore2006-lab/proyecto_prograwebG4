using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoQ3Backend.DTOs;
using ProyectoQ3Backend.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProyectoQ3Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateUserDto dto)
    {
        try
        {
            await _userService.UpdateAsync(GetUserId(), dto);
            return Ok(new { message = "Perfil actualizado correctamente." });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPatch]
    public async Task<IActionResult> Patch([FromBody] PatchUserDto dto)
    {
        try
        {
            await _userService.PatchAsync(GetUserId(), dto);
            return Ok(new { message = "Perfil actualizado parcialmente." });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete()
    {
        try
        {
            await _userService.DeleteAsync(GetUserId());
            return Ok(new { message = "Cuenta eliminada de Firebase Authentication y Firestore." });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    private string GetUserId()
    {
        return User.FindFirstValue("user_id")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("El token no contiene el UserId.");
    }
}
