using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using ZstdSharp.Unsafe;

namespace SearchService;

public static class RoutesExtensions
{
    public static void MapRoutes(this IEndpointRouteBuilder endpointRoute)
    {
        endpointRoute.MapGet("/api/search", async (HttpRequest request, int pageNumber = 1, int pageSize = 4) =>
        {
            var searchParams = new SearchParams
            {
                SearchTerm = request.Query["SearchTerm"],
                FilterBy = request.Query["FilterBy"],
                OrderBy = request.Query["OrderBy"],
                Seller = request.Query["Seller"],
                Winner = request.Query["Winner"],
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var query = DB.PagedSearch<Item, Item>();

            if (!string.IsNullOrEmpty(searchParams.SearchTerm))
            {
                query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
            }

            query = searchParams.OrderBy switch
            {
                "make" => query.Sort(x => x.Ascending(a => a.Make)),
                "new" => query.Sort(x => x.Descending(a => a.CreatedAt)),
                _ => query.Sort(x => x.Ascending(a => a.AuctionEnd)),
            };

            query = searchParams.FilterBy switch
            {
                "finished" => query.Match(x => x.AuctionEnd < DateTime.UtcNow),
                "endingSoon" => query.Match(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6) && x.AuctionEnd > DateTime.UtcNow),
                _ => query.Match(x => x.AuctionEnd > DateTime.UtcNow),
            };

            if (!string.IsNullOrEmpty(searchParams.Seller))
            {
                query.Match(x => x.Seller == searchParams.Seller);
            }

            if (!string.IsNullOrEmpty(searchParams.Winner))
            {
                query.Match(x => x.Seller == searchParams.Winner);
            }

            query.PageNumber(searchParams.PageNumber);
            query.PageSize(searchParams.PageSize);

            var result = await query.ExecuteAsync();

            return Results.Ok(new
            {
                results = result.Results,
                pageCount = result.PageCount,
                totalCount = result.TotalCount
            });
        })
        .WithName("SearchItems");
    }
}
