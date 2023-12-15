var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IProductStore, ProductStore>();
var app = builder.Build();

static IEnumerable<int> ParseProductIdsFromQueryString(string productIdsString) => productIdsString.Split(',').Select(s => s.Replace("[", "").Replace("]", "")).Select(int.Parse);

app.MapGet("/products", (string productIds) =>
{
    var productStore = app.Services.GetRequiredService<IProductStore>();
    var products = productStore.GetProductsByIds(ParseProductIdsFromQueryString(productIds));
    return Results.Ok(products);
});

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