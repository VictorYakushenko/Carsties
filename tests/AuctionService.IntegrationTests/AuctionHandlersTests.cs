using System.Net;
using System.Net.Http.Json;
using AuctionService.DTOs;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Util;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests;

[Collection("Shared collection")]
public class AuctionHandlersTests : IAsyncLifetime
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _httpClient;

    public AuctionHandlersTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        DbHelper.ReinitDbForTest(db);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetAuctions_ShouldReturn3Auctions()
    {
        var response = await _httpClient.GetFromJsonAsync<List<AuctionDto>>("api/auctions");

        Assert.Equal(3, response.Count);
    }

    [Fact]
    public async Task GetAuctionById_WithValidId_ShouldReturnAuction()
    {
        var response = await _httpClient.GetFromJsonAsync<AuctionDto>($"api/auctions/{DbHelper.GT_ID}");

        Assert.Equal("GT", response.Model);
    }

    [Fact]
    public async Task GetAuctionById_WithInvalidId_ShouldReturn404()
    {
        var response = await _httpClient.GetAsync($"api/auctions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAuctionById_WithInvalidGuid_ShouldReturn400()
    {
        var response = await _httpClient.GetAsync($"api/auctions/notaguid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuction_WithNoAuth_ShouldReturn401()
    {
        var auction = new CreateAuctionDto { Make = "test" };

        var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuction_WithAuth_ShouldReturn201()
    {
        var auction = DataHelper.GetAuctionForCreate();

        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdAuction = await response.Content.ReadFromJsonAsync<AuctionDto>();

        Assert.Equal("bob", createdAuction.Seller);
    }

    [Fact]
    public async Task CreateAuction_WithInvalidCreateAuctionDto_ShouldReturn400()
    {
        var auction = DataHelper.GetAuctionForCreate();
        auction.Make = null;
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_WithValidUpdateDtoAndUser_ShouldReturn200()
    {
        var updateAuction = new UpdateAuctionDto { Make = "Updated" };
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));


        var response = await _httpClient.PutAsJsonAsync($"api/auctions/{DbHelper.GT_ID}", updateAuction);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_WithValidUpdateDtoAndInvalidUser_ShouldReturn403()
    {
        var updateAuction = new UpdateAuctionDto { Make = "Updated" };
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("notbob"));


        var response = await _httpClient.PutAsJsonAsync($"api/auctions/{DbHelper.GT_ID}", updateAuction);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}