using AuctionService.Handlers;

namespace AuctionService.Extensions;

public static class RoutesExtensions
{
    public static void MapRoutes(this IEndpointRouteBuilder endpointRoute)
    {
        endpointRoute.MapGet("api/auctions", AuctionHandlers.GetAllAuctionsHandler)
            .WithName("GetAllAuctions");

        endpointRoute.MapGet("api/auctions/{id}", AuctionHandlers.GetAuctionByIdHandler)
            .WithName("GetAuctionById");

        endpointRoute.MapPost("api/auctions", AuctionHandlers.CreateAuctionHandler)
            .WithName("CreateAuction")
            .RequireAuthorization();

        endpointRoute.MapPut("api/auctions/{id}", AuctionHandlers.UpdateAuctionHandler)
            .WithName("UpdateAuction")
            .RequireAuthorization();

        endpointRoute.MapDelete("api/auctions/{id}", AuctionHandlers.DeleteAuctionHandler)
            .WithName("DeleteAuction")
            .RequireAuthorization();
    }
}
