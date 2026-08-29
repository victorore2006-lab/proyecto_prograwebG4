using System;
using System.ComponentModel.DataAnnotations;

namespace ProyectoQ3Backend.DTOs;

public class RegisterDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public DateTime? BirthDate { get; set; }

    [Required]
    public string Country { get; set; } = string.Empty;

    [Required]
    public string Bio { get; set; } = string.Empty;
}
