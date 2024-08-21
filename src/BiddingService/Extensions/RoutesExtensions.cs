using AutoMapper;
using BiddingService.DTOs;
using BiddingService.Models;
using BiddingService.Services;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Entities;

namespace BiddingService.Extensions;

public static class RoutesExtensions
{
    public static void MapRoutes(this IEndpointRouteBuilder endpointRoute)
    {
        endpointRoute.MapPost("/api/bids", [Authorize]
        async (string auctionId, int amount, HttpContext httpContext, IMapper mapper,
        IPublishEndpoint publishEndpoint, GrpcAuctionClient grpcClient) =>
        {
            var auction = await DB.Find<Auction>().OneAsync(auctionId);

            if (auction == null)
            {
                auction = grpcClient.GetAuction(auctionId);

                if (auction == null) return Results.BadRequest("Cannot accept bids on this auctions at this time");
            }

            if (auction.Seller == httpContext.User.Identity.Name)
            {
                return Results.BadRequest("You cannot bid on your own auction");
            }

            var bid = new Bid
            {
                Amount = amount,
                AuctionId = auctionId,
                Bidder = httpContext.User.Identity.Name
            };

            if (auction.AuctionEnd < DateTime.UtcNow)
            {
                bid.BidStatus = BidStatus.Finished;
            }
            else
            {
                var highBid = await DB.Find<Bid>()
                    .Match(a => a.AuctionId == auctionId)
                    .Sort(b => b.Descending(x => x.Amount))
                    .ExecuteFirstAsync();

                if (highBid != null && amount > highBid.Amount || highBid == null)
                {
                    bid.BidStatus = amount > auction.ReservePrice ? BidStatus.Accepted : BidStatus.AcceptedBelowReserve;
                }

                if (highBid != null && amount <= highBid.Amount)
                {
                    bid.BidStatus = BidStatus.TooLow;
                }
            }

            await DB.SaveAsync(bid);

            await publishEndpoint.Publish(mapper.Map<BidPlaced>(bid));

            return Results.Ok(mapper.Map<BidDto>(bid));
        })
        .WithName("PlaceBid");

        endpointRoute.MapGet("/api/bids/{auctionId}", async (string auctionId, IMapper mapper) =>
        {
            var bids = await DB.Find<Bid>()
            .Match(a => a.AuctionId == auctionId)
            .Sort(b => b.Descending(a => a.BidTime))
            .ExecuteAsync();

            return bids.Select(mapper.Map<BidDto>).ToList();
        })
        .WithName("CetBidsForAuction");
    }
}
