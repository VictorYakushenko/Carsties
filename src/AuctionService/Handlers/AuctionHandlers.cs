using System;
using System.ComponentModel.DataAnnotations;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;

namespace AuctionService.Handlers;

public class AuctionHandlers
{
    public static Task<List<AuctionDto>> GetAllAuctionsHandler(string date, IAuctionRepository repo)
        => repo.GetAuctionsAsync(date);

    public static async Task<IResult> GetAuctionByIdHandler(Guid id, IAuctionRepository repo)
    {
        var auction = await repo.GetAuctionByIdAsync(id);
        if (auction == null) return Results.NotFound();
        return Results.Ok(auction);
    }

    public static async Task<IResult> CreateAuctionHandler(CreateAuctionDto auctionDto, IAuctionRepository repo, IMapper mapper, IPublishEndpoint publishEndpoint, HttpContext httpContext)
    {
        var validationContext = new ValidationContext(auctionDto);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (!Validator.TryValidateObject(auctionDto, validationContext, validationResults, true))
        {
            return Results.BadRequest(validationResults);
        }

        var auction = mapper.Map<Auction>(auctionDto);

        auction.Seller = httpContext.User.Identity?.Name;

        repo.AddAuction(auction);

        var newAuction = mapper.Map<AuctionDto>(auction);

        await publishEndpoint.Publish(mapper.Map<AuctionCreated>(newAuction));

        var result = await repo.SaveChangesAsync();

        if (!result) return Results.BadRequest("Could not save changes to the DB");

        return Results.CreatedAtRoute("GetAuctionById", new { auction.Id }, newAuction);
    }

    public static async Task<IResult> UpdateAuctionHandler(Guid id, UpdateAuctionDto updateAuctionDto, IAuctionRepository repo, IMapper mapper, IPublishEndpoint publishEndpoint, HttpContext httpContext)
    {
        var auction = await repo.GetAuctionEntityById(id);

        if (auction == null) return Results.NotFound();

        if (auction.Seller != httpContext.User.Identity?.Name) return Results.Forbid();

        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

        await publishEndpoint.Publish(mapper.Map<AuctionUpdated>(auction));

        var result = await repo.SaveChangesAsync();

        if (result) return Results.Ok();

        return Results.BadRequest("Problem saving changes");
    }

    public static async Task<IResult> DeleteAuctionHandler(Guid id, IAuctionRepository repo, IPublishEndpoint publishEndpoint, HttpContext httpContext)
    {
        var auction = await repo.GetAuctionEntityById(id);

        if (auction == null) return Results.NotFound();

        if (auction.Seller != httpContext.User.Identity?.Name) return Results.Forbid();

        repo.RemoveAuction(auction);
        await publishEndpoint.Publish<AuctionDeleted>(new { Id = auction.Id.ToString() });

        var result = await repo.SaveChangesAsync();

        if (result) return Results.Ok();

        return Results.BadRequest("Could not update DB");
    }
}
