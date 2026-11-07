namespace GmachManagement.Core.Entities;
public class Gmach {
 public int Id { get; set; }
 public string Name { get; set; } = string.Empty;
 public string City { get; set; } = string.Empty;
 public string? Description { get; set; }
 public int OwnerId { get; set; }
 public User Owner { get; set; } = null!;
 public ICollection<Item> Items { get; set; } = new List<Item>();
}
