using System.Net.Http.Headers;
using System.Text.Json;

var start = await GetStartIdFromDatastore();
var end = 100;
var client = new HttpClient();

client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
using var resp = await client.GetAsync(new Uri($"http://special-offers:5002/events?startRange={start}&endRange={end}"));

await ProcessEvents(await resp.Content.ReadAsStreamAsync());
await SaveStartIdToDataStore(start);

Task<long> GetStartIdFromDatastore() => Task.FromResult(0L);

async Task ProcessEvents(Stream content)
{
    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    };

    var events = await JsonSerializer.DeserializeAsync<SpecialOfferEvent[]>(content, options) ?? new SpecialOfferEvent[0];
    foreach (var @event in events)
    {
        Console.WriteLine(@event);
        start = Math.Max(start, @event.SequenceNumber + 1);
    }
}

Task SaveStartIdToDataStore(long startId) => Task.CompletedTask;

public record SpecialOfferEvent(long SequenceNumber, DateTimeOffset OccurredAt, string Name, object Content);

