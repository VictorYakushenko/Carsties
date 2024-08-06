using System.Net;
using Polly;
using Polly.Extensions.Http;
using SearchService;
using SearchService.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthorization();
builder.Services.AddHttpClient<AuctionServiceHttpClient>()
    .AddPolicyHandler(GetPolicy());

var app = builder.Build();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapRoutes();

app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await app.InitDbAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
});

app.Run();


static IAsyncPolicy<HttpResponseMessage> GetPolicy()
    => HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
        .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));