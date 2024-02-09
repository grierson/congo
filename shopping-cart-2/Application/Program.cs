using System.Text.Json;
using Marten;
using Marten.Events.Aggregation;
using Marten.Events.Projections;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMarten(options =>
{
    options.Connection("Host=localhost;Port=5432;Database=cart;Username=postgres;password=postgres;");
    options.AutoCreateSchemaObjects = AutoCreate.All;

    options.Events.AddEventType(typeof(CreatedShoppingCartEvent));
    options.Events.AddEventType(typeof(AddedShoppingCartItemEvent));

    options.Projections.Add<ShoppingCartProjection>(ProjectionLifecycle.Inline);
})
.UseLightweightSessions();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/health", () =>
{
    return "Healthy";
});

app.MapGet("/events", (IDocumentSession session) =>
{
    Console.WriteLine("IRUN");
    var events = session.Events.QueryAllRawEvents().ToList();
    Console.WriteLine("IRUN 2");
    Console.WriteLine("----");
    Console.WriteLine(events);
    foreach (var @event in events)
    {
        Console.WriteLine(@event.Data.ToString());
    }
    Console.WriteLine("----");
    /* var json = JsonSerializer.Serialize(events); */
    /* Console.WriteLine("IRUN 3"); */
    return "Events";
});

app.MapGet("/cart", () =>
{
    return "Cart";
});

app.MapPost("/cart", async (IDocumentSession session, CreateShoppingCartRequest request) =>
{
    var createEvent = new CreatedShoppingCartEvent(Guid.NewGuid(), request.Name);
    session.Events.StartStream<ShoppingCart>(createEvent);
    await session.SaveChangesAsync();
    return "Created";
});

app.MapGet("/cart/{id:guid}", (IQuerySession session, Guid id) =>
{
    Console.WriteLine("ID: " + id);
    return session.Events.AggregateStreamAsync<ShoppingCart>(id);
});

app.Run();

public record CreateShoppingCartRequest(string Name);
public record AddShoppingCartItemRequest(string Name, int Quantity);

public record ShoppingCartItem(string Name, int Quantity);
public class ShoppingCart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public IEnumerable<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();
}

public class ShoppingCartProjection : SingleStreamProjection<ShoppingCart>
{
    public ShoppingCartProjection() { }

    public void Apply(CreatedShoppingCartEvent @event, ShoppingCart cart)
    {
        cart.Name = @event.Name;
    }

    public void Apply(AddedShoppingCartItemEvent @event, ShoppingCart cart)
    {
        cart.Items.Append(new ShoppingCartItem(@event.Name, @event.Quantity));
    }

    public ShoppingCart Create(CreatedShoppingCartEvent @event)
    {
        return new ShoppingCart();
    }
}

public record CreatedShoppingCartEvent(Guid Id, string Name);
public record AddedShoppingCartItemEvent(string Name, int Quantity);
