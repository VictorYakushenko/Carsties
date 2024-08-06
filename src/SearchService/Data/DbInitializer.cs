using MongoDB.Driver;
using MongoDB.Entities;

namespace SearchService.Data;

public static class DbInitializer
{
    public static async Task InitDbAsync(this WebApplication app)
    {
        await DB.InitAsync("SearchDb", MongoClientSettings.FromConnectionString(app.Configuration.GetConnectionString("MongoDbConnection")));

        await DB.Index<Item>()
            .Key(x => x.Make, KeyType.Text)
            .Key(x => x.Model, KeyType.Text)
            .Key(x => x.Color, KeyType.Text)
            .CreateAsync();

        var count = await DB.CountAsync<Item>();

        using var scope = app.Services.CreateScope();

        var httpClient = scope.ServiceProvider.GetRequiredService<AuctionServiceHttpClient>();

        var items = await httpClient.GetItemsFromSearchDb();

        Console.WriteLine(items.Count + " returned from auction service");

        if (items.Count > 0)
        {
            await DB.SaveAsync(items);
        }
    }
}
