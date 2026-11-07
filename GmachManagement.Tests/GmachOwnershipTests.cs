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
}
