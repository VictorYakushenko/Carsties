using AuctionService.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionService;

//dotnet ef migrations add "InitialCreate" -o Data/Migrations
public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Auction> Auctions { get; set; }
}
