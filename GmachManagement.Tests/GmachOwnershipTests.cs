using GmachManagement.Core.Entities;
using GmachManagement.Core.Enums;
using GmachManagement.Data.Context;
using GmachManagement.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace GmachManagement.Tests;
public class GmachOwnershipTests {
 private sealed class TestDbContext : AppDbContext {
  public TestDbContext(DbContextOptions<AppDbContext> options):base(options){}
  protected override void OnModelCreating(ModelBuilder builder){base.OnModelCreating(builder);builder.Entity<ItemCopy>().Ignore(c=>c.RowVersion);}
 }

 [Fact] public async Task EmailLookupIsCaseInsensitive(){
  await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
  var options=new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
  await using var db=new TestDbContext(options);await db.Database.EnsureCreatedAsync();
  db.Users.Add(new User{Id=301,Name="Test",Email="registered@example.com",PasswordHash="test"});
  await db.SaveChangesAsync();
  var users=new UserRepository(db);
  Assert.NotNull(await users.GetByEmailAsync(" REGISTERED@EXAMPLE.COM ",CancellationToken.None));
 }
 [Fact] public async Task ExcelBatchIsAssignedOnlyToItsGmach(){
  await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
  var options=new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
  await using var db=new TestDbContext(options);await db.Database.EnsureCreatedAsync();
  db.Users.Add(new User{Id=201,Name="Owner",Email="owner@test.invalid",Role=UserRole.GmachAdmin});
  db.Gmachs.Add(new Gmach{Id=201,OwnerId=201,Name="Tools",City="Jerusalem"});
  await db.SaveChangesAsync();
  var repo=new GmachRepository(db);
  var count=await repo.ImportItemsAsync(201,new List<(string Name,string Description,int Quantity)>{("Drill","Electric",3),("Saw","Hand",2)},CancellationToken.None);
  Assert.Equal(2,count);
  Assert.Equal(5,await db.ItemCopies.CountAsync(c=>c.Item.GmachId==201));
  Assert.Equal(3,await db.ItemCopies.CountAsync(c=>c.Item.Name=="Drill" && c.Status==ItemStatus.Available));
  Assert.Equal(2,await db.Items.CountAsync(i=>i.GmachId==201));
  Assert.Equal(0,await db.Items.CountAsync(i=>i.GmachId!=null && i.GmachId!=201));
 }
 [Fact] public async Task ReimportAndManualAddIncreaseCopiesWithoutDuplicateItems(){
  await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
  var options=new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
  await using var db=new TestDbContext(options);await db.Database.EnsureCreatedAsync();
  db.Users.Add(new User{Id=401,Name="Owner",Email="merge@test.invalid",Role=UserRole.GmachAdmin});
  db.Gmachs.Add(new Gmach{Id=401,OwnerId=401,Name="Tools",City="Jerusalem"});
  await db.SaveChangesAsync();
  var repo=new GmachRepository(db);
  await repo.ImportItemsAsync(401,new List<(string Name,string Description,int Quantity)>{("Drill","Electric",3)},CancellationToken.None);
  await repo.ImportItemsAsync(401,new List<(string Name,string Description,int Quantity)>{(" drill ","electric",2),("Drill","Electric",4)},CancellationToken.None);
  await repo.AddItemAsync(401,"DRILL","ELECTRIC",CancellationToken.None);
  Assert.Equal(1,await db.Items.CountAsync(i=>i.GmachId==401));
  Assert.Equal(10,await db.ItemCopies.CountAsync(c=>c.Item.GmachId==401));
 }
 [Fact] public async Task CitySearchAndOwnerIsolation(){
  await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
  var options=new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
  await using var db=new TestDbContext(options);await db.Database.EnsureCreatedAsync();
  db.Users.AddRange(new User{Id=101,Name="A",Email="owner-a@test.invalid",Role=UserRole.GmachAdmin},new User{Id=102,Name="B",Email="owner-b@test.invalid",Role=UserRole.GmachAdmin});
  db.Gmachs.AddRange(new Gmach{Id=101,OwnerId=101,Name="Tools",City="Jerusalem"},new Gmach{Id=102,OwnerId=102,Name="Medical",City="Haifa"});
  db.Items.AddRange(new Item{Id=101,Name="Drill",GmachId=101},new Item{Id=102,Name="Chair",GmachId=102});
  await db.SaveChangesAsync();
  var repo=new GmachRepository(db);
  var found=await repo.SearchAsync("Jerusalem",null,CancellationToken.None);
  Assert.Single(found);Assert.Equal(101,found[0].OwnerId);
  Assert.True(await repo.OwnsItemAsync(101,101,CancellationToken.None));
  Assert.False(await repo.OwnsItemAsync(101,102,CancellationToken.None));
 }

 [Fact]
 public async Task PublicDirectoryMatchesCityNameAndAvailableInventoryInOneSearch()
 {
  await using var connection = new SqliteConnection("Data Source=:memory:");
  await connection.OpenAsync();
  var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
  await using var db = new TestDbContext(options);
  await db.Database.EnsureCreatedAsync();

  db.Gmachs.AddRange(Enumerable.Range(501, 4).Select(id => new Gmach
  {
   Id = id,
   Owner = new User { Id = id, Name = "Owner", Email = $"owner-{id}@test.invalid" },
   Name = id == 502 ? "Drill Gmach" : "Equipment",
   City = id == 501 ? "Jerusalem" : "Haifa"
  }));
  db.Items.AddRange(
   new Item { Id = 503, Name = "Drill", GmachId = 503, Copies =
    [new ItemCopy { Status = ItemStatus.Available }, new ItemCopy { Status = ItemStatus.Borrowed }] },
   new Item { Id = 504, Name = "Drill", GmachId = 504, Copies = [new ItemCopy { Status = ItemStatus.Borrowed }] },
   new Item { Id = 505, Name = "Drill", GmachId = 503, IsActive = false, Copies =
    [new ItemCopy { Status = ItemStatus.Available }] });
  await db.SaveChangesAsync();

  var repository = new GmachRepository(db);
  var results = await repository.SearchDirectoryAsync("Jerusalem", "Drill", default);
  Assert.Equal([501, 502, 503], results.Select(gmach => gmach.Id));

  var details = await repository.GetPublicDetailsAsync(503, default);
  Assert.NotNull(details);
  var item = Assert.Single(details.Items);
  Assert.Equal(503, item.Id);
  Assert.Equal(1, item.AvailableCopies);
  Assert.Null(await repository.GetPublicDetailsAsync(-1, default));
 }
}
