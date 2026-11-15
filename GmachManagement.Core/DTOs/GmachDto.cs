using System.ComponentModel.DataAnnotations;
namespace GmachManagement.Core.DTOs;
public class GmachDto {
 public int Id { get; set; }
 [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
 [Required, StringLength(100)] public string City { get; set; } = string.Empty;
 public string? Description { get; set; }
}
