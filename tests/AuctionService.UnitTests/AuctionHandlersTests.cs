using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AuctionService.Handlers;
using AuctionService.RequestHelpers;
using AutoFixture;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuctionService.UnitTests;

public class AuctionHandlersTests
{
    private readonly Mock<IAuctionRepository> _auctionRepo;
    private readonly Mock<IPublishEndpoint> _publishEndpoint;
    private readonly Fixture _fixture;
    private readonly IMapper _mapper;
    private readonly HttpContext _httpContext;

    public AuctionHandlersTests()
    {
        _fixture = new Fixture();
        _auctionRepo = new Mock<IAuctionRepository>();
        _publishEndpoint = new Mock<IPublishEndpoint>();
        var mockMapper = new MapperConfiguration(mc =>
        {
            mc.AddMaps(typeof(MappingProfiles).Assembly);
        }).CreateMapper().ConfigurationProvider;
        _mapper = new Mapper(mockMapper);
        _httpContext = new DefaultHttpContext
        {
            User = Heplers.GetClaimsPrincipal()
        };
    }

    [Fact]
    public async Task GetAuctions_WithNoParams_Returns10Auctions()
    {
        var auctions = _fixture.CreateMany<AuctionDto>(10).ToList();
        _auctionRepo.Setup(repo => repo.GetAuctionsAsync(null)).ReturnsAsync(auctions);

        var result = await AuctionHandlers.GetAllAuctionsHandler(null, _auctionRepo.Object);

        Assert.IsType<List<AuctionDto>>(result);

        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task GetAuctionById_WithValidGuid_ReturnsAuction()
    {
        var auction = _fixture.Create<AuctionDto>();
        _auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(auction);

        var result = await AuctionHandlers.GetAuctionByIdHandler(auction.Id, _auctionRepo.Object);

        var okResult = Assert.IsType<Ok<AuctionDto>>(result);

        Assert.Equal(auction.Make, okResult.Value.Make);
    }

    [Fact]
    public async Task GetAuctionById_WithInvalidGuid_ReturnsNotFound()
    {
        _auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(value: null);

        var result = await AuctionHandlers.GetAuctionByIdHandler(Guid.NewGuid(), _auctionRepo.Object);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task CreateAuction_WithValidDto_ReturnsCreatedAtAction()
    {
        var auctionDto = _fixture.Create<CreateAuctionDto>();

        _auctionRepo.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

        var result = await AuctionHandlers.CreateAuctionHandler
        (auctionDto, _auctionRepo.Object, _mapper, _publishEndpoint.Object, _httpContext);

        Assert.NotNull(result);
        var createdResult = Assert.IsType<CreatedAtRoute<AuctionDto>>(result);

        Assert.Equal("GetAuctionById", createdResult.RouteName);
        Assert.IsType<AuctionDto>(createdResult.Value);
    }

    [Fact]
    public async Task CreateAuction_FailedSave_Returns400BadRequest()
    {
        var auctionDto = _fixture.Create<CreateAuctionDto>();
        _auctionRepo.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(false);

        var result = await AuctionHandlers.CreateAuctionHandler
        (auctionDto, _auctionRepo.Object, _mapper, _publishEndpoint.Object, _httpContext);

        Assert.IsType<BadRequest<string>>(result);
    }

    [Fact]
    public async Task UpdateAuction_WithUpdateAuctionDto_ReturnsOkResponse()
    {
        var auction = _fixture.Build<Auction>().Without(x => x.Item).Create();
        auction.Item = _fixture.Build<Item>().Without(x => x.Auction).Create();
        auction.Seller = "test";
        var updateDto = _fixture.Create<UpdateAuctionDto>();

        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(auction);
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

        var result = await AuctionHandlers.UpdateAuctionHandler
        (auction.Id, updateDto, _auctionRepo.Object, _mapper, _publishEndpoint.Object, _httpContext);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task UpdateAuction_WithInvalidUser_Returns403Forbid()
    {
        var auction = _fixture.Build<Auction>().Without(x => x.Item).Create();
        auction.Seller = "non-test";
        var updateDto = _fixture.Create<UpdateAuctionDto>();

        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(auction);

        var result = await AuctionHandlers.UpdateAuctionHandler
        (auction.Id, updateDto, _auctionRepo.Object, _mapper, _publishEndpoint.Object, _httpContext);

        Assert.IsType<ForbidHttpResult>(result);
    }

    [Fact]
    public async Task UpdateAuction_WithInvalidGuid_ReturnsNotFound()
    {
        var auction = _fixture.Build<Auction>().Without(x => x.Item).Create();
        auction.Seller = "test";
        var updateDto = _fixture.Create<UpdateAuctionDto>();

        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(value: null);

        var result = await AuctionHandlers.UpdateAuctionHandler
        (auction.Id, updateDto, _auctionRepo.Object, _mapper, _publishEndpoint.Object, _httpContext);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithValidUser_ReturnsOkResponse()
    {
        var auction = _fixture.Build<Auction>().Without(x => x.Item).Create();
        auction.Seller = "test";

        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(auction);
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

        var result = await AuctionHandlers.DeleteAuctionHandler
        (auction.Id, _auctionRepo.Object, _publishEndpoint.Object, _httpContext);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithInvalidGuid_Returns404Response()
    {
        var auction = _fixture.Build<Auction>().Without(x => x.Item).Create();
        auction.Seller = "test";

        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(value: null);

        var result = await AuctionHandlers.DeleteAuctionHandler
        (auction.Id, _auctionRepo.Object, _publishEndpoint.Object, _httpContext);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithInvalidUser_Returns403Response()
    {
        var auction = _fixture.Build<Auction>().Without(x => x.Item).Create();
        auction.Seller = "not-test";

        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(auction);

        var result = await AuctionHandlers.DeleteAuctionHandler
        (auction.Id, _auctionRepo.Object, _publishEndpoint.Object, _httpContext);

        Assert.IsType<ForbidHttpResult>(result);
    }
}
