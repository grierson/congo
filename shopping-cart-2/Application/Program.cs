var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IShoppingCartRepository, ShoppingCartRepository>();
var app = builder.Build();

app.UseHttpsRedirection();

app
.MapGroup("/shoppingcart")
.ShoppingCartRoutes();

app.MapGet("/health", () =>
{
    return "Healthy";
});

app.MapGet("/events", (IShoppingCartRepository repository) =>
{
    return repository.Events();
});

app.Run();


public interface IShoppingCartRepository
{
    Task<ShoppingCart> Project(int id);
    void Raise(Event domainEvent);
    IEnumerable<Event> Events();
}

public record CreateShoppingCartRequest(string Name);
public record AddShoppingCartItemRequest(string Name, int Quantity);
public record Event(Guid Id, int StreamId, DateTime OccurredAt, string Type, object Payload);
public record CreatedShoppingCartEvent(int CartId, string Name) : Event(Guid.NewGuid(), CartId, DateTime.UtcNow, "ShoppingCartCreated", new { Name });
public record AddedShoppingCartItemEvent(int CartId, string Name, int Quantity) : Event(Guid.NewGuid(), CartId, DateTime.UtcNow, "AddedShoppingCartItem", new { Name, Quantity });

public record ShoppingCartItem(string Name, int Quantity);
public record ShoppingCart
{
    public int Id { get; init; } = 1;
    public string Name { get; init; } = string.Empty;
    public IEnumerable<ShoppingCartItem> Items { get; init; } = new List<ShoppingCartItem>();
}

public class ShoppingCartRepository : IShoppingCartRepository
{
    private List<Event> _events = new List<Event>();

    public IEnumerable<Event> Events()
    {
        return _events;
    }

    public Task<ShoppingCart> Project(int id)
    {
        var events = _events.Where(e => e.StreamId == id).ToList();

        var state = events.Aggregate(new ShoppingCart(), (state, @event) =>
        {
            switch (@event)
            {
                case CreatedShoppingCartEvent createdShoppingCartEvent:
                    return state with { Name = createdShoppingCartEvent.Name };
                case AddedShoppingCartItemEvent addedShoppingCartItemEvent:
                    return state with { Items = new List<ShoppingCartItem> { new ShoppingCartItem(addedShoppingCartItemEvent.Name, addedShoppingCartItemEvent.Quantity) } };
                default:
                    return state;
            }
        });

        return Task.FromResult<ShoppingCart>(state);
    }

    public void Raise(Event domainEvent)
    {
        _events.Add(domainEvent);
    }
}

public static class RouteGroupExtensions
{
    public static RouteGroupBuilder ShoppingCartRoutes(this RouteGroupBuilder route)
    {
        route.MapGet("/{id}", async (IShoppingCartRepository repository, int id) =>
        {
            return await repository.Project(id);
        });

        route.MapPost("/", async (IShoppingCartRepository repository, CreateShoppingCartRequest request) =>
        {
            var @event = new CreatedShoppingCartEvent(1, request.Name);
            repository.Raise(@event);
            return await repository.Project(1);
        });

        route.MapPost("/{id}/items", async (IShoppingCartRepository repository, int id, AddShoppingCartItemRequest request) =>
        {
            var @event = new CreatedShoppingCartEvent(1, "hello");
            repository.Raise(@event);
            return await repository.Project(1);
        });

        return route;
    }
}
