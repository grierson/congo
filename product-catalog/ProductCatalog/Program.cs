var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IProductStore, ProductStore>();
builder.Services.AddResponseCaching();
var app = builder.Build();
app.UseResponseCaching();

static IEnumerable<int> ParseProductIdsFromQueryString(string productIdsString) => productIdsString.Split(',').Select(s => s.Replace("[", "").Replace("]", "")).Select(int.Parse);

app.MapGet("/products", (HttpContext context, string productIds) =>
{
    var productStore = app.Services.GetRequiredService<IProductStore>();
    var products = productStore.GetProductsByIds(ParseProductIdsFromQueryString(productIds));

    var response = context.Response;
    response.Headers.Add("Cache-Control", "public, max-age=60");

    return Results.Ok(new
    {
        Date = DateTime.Now,
        Products = products
    });
}).CacheOutput();

app.Run();

public interface IProductStore
{
    IEnumerable<ProductCatalogProduct> GetProductsByIds(IEnumerable<int> productIds);
}

public class ProductStore : IProductStore
{
    public IEnumerable<ProductCatalogProduct> GetProductsByIds(IEnumerable<int> productIds) =>
        productIds.Select(id => new ProductCatalogProduct(id, "foo" + id, "bar", 0));
}

public record ProductCatalogProduct(int ProductId, string ProductName, string Description, double Price);
