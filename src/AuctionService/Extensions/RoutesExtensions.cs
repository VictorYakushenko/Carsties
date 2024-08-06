using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Extensions;

public static class RoutesExtensions
{
    public static void MapRoutes(this IEndpointRouteBuilder endpointRoute)
    {
        endpointRoute.MapGet("api/auctions", async (string date, AuctionDbContext context, IMapper mapper) =>
        {
            var query = context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

            if (!string.IsNullOrEmpty(date))
            {
                query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
            }

            return await query.ProjectTo<AuctionDto>(mapper.ConfigurationProvider).ToListAsync();
        })
        .WithName("GetAllAuctions");

        endpointRoute.MapGet("api/auctions/{id}", async (Guid id, AuctionDbContext context, IMapper mapper) =>
        {
            var auction = await context.Auctions
                .Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (auction == null) return Results.NotFound();

            return Results.Ok(mapper.Map<AuctionDto>(auction));
        })
        .WithName("GetAuctionById");

        endpointRoute.MapPost("api/auctions", async (CreateAuctionDto auctionDto, AuctionDbContext context, IMapper mapper) =>
        {
            var auction = mapper.Map<Auction>(auctionDto);
            auction.Seller = "test";
            context.Auctions.Add(auction);
            var result = await context.SaveChangesAsync() > 0;

            if (!result) return Results.BadRequest("Could not save changes to the DB");

            return Results.CreatedAtRoute("GetAuctionById", new { auction.Id }, mapper.Map<AuctionDto>(auction));
        })
        .WithName("CreateAuction");

        endpointRoute.MapPut("api/auctions/{id}", async (Guid id, UpdateAuctionDto updateAuctionDto, AuctionDbContext context, IMapper mapper) =>
       {
           var auction = await context.Auctions
               .Include(x => x.Item)
               .FirstOrDefaultAsync(x => x.Id == id);

           if (auction == null) return Results.NotFound();

           auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
           auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
           auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
           auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
           auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

           var result = await context.SaveChangesAsync() > 0;

           if (result) return Results.Ok();

           return Results.BadRequest("Problem saving changes");
       })
       .WithName("UpdateAuction");

        endpointRoute.MapDelete("api/auctions/{id}", async (Guid id, AuctionDbContext context) =>
        {
            var auction = await context.Auctions
                .FindAsync(id);

            if (auction == null) return Results.NotFound();

            context.Auctions.Remove(auction);

            var result = await context.SaveChangesAsync() > 0;

            if (result) return Results.Ok();

            return Results.BadRequest("Could not update DB");
        })
        .WithName("DeleteAuction");
    }
}
