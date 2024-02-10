using Marten;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMarten(options =>
{
    options.Connection("Host=localhost;Port=5432;Database=cart;Username=postgres;password=postgres;");
    options.AutoCreateSchemaObjects = AutoCreate.All;

    options.Events.AddEventType(typeof(CreatedShoppingCartEvent));
    options.Events.AddEventType(typeof(AddedShoppingCartItemEvent));
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
    var events = session.Events.QueryAllRawEvents().ToList();
    Console.WriteLine("----");
    foreach (var @event in events)
    {
        Console.WriteLine(@event.Data.ToString());
    }
    Console.WriteLine("----");
    return "Events";
});

app.MapGet("/cart", () =>
{
    return "Cart";
});

app.MapPost("/cart", async (IDocumentSession session, CreateShoppingCartRequest request) =>
{
    var id = Guid.NewGuid();
    var createEvent = new CreatedShoppingCartEvent(request.Name);
    session.Events.StartStream<ShoppingCart>(id, createEvent);
    await session.SaveChangesAsync();
    return id;
});

app
.MapGet("/cart/{id:guid}", async (IQuerySession session, Guid id) =>
{
    return await session.Events.AggregateStreamAsync<ShoppingCart>(id);
});

app
.MapPost("/cart/{id:guid}/items",
        async (IDocumentSession session, AddShoppingCartItemRequest request, Guid id) =>
{
    var @event = new AddedShoppingCartItemEvent(request.Name, request.Quantity);
    session.Events.Append(id, @event);
    await session.SaveChangesAsync();
    return "Added";
});

app.Run();

public record CreateShoppingCartRequest(string Name);
public record AddShoppingCartItemRequest(string Name, int Quantity);

public record ShoppingCartItem(string Name, int Quantity);
public record ShoppingCart(Guid Id, string Name, IEnumerable<ShoppingCartItem> Items)
{
    public static ShoppingCart Create(CreatedShoppingCartEvent @event) =>
        new ShoppingCart(Guid.NewGuid(), @event.Name, Enumerable.Empty<ShoppingCartItem>());

    public ShoppingCart Apply(CreatedShoppingCartEvent @event) =>
        this with { Name = @event.Name };

    public ShoppingCart Apply(AddedShoppingCartItemEvent @event) =>
        this with { Items = this.Items.Append(new ShoppingCartItem(@event.Name, @event.Quantity)) };
}

public record CreatedShoppingCartEvent(string Name);
public record AddedShoppingCartItemEvent(string Name, int Quantity);
