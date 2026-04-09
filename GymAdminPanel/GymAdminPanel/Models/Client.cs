using System;
using System.ComponentModel.DataAnnotations;

namespace GymAdminPanel.Models;

public class Client
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role {  get; set; } = string.Empty;

    public DateTime RegistrationDate { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;
}