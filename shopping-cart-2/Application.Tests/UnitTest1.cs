namespace Application.Tests;

public class UnitTest1
{
    [Fact]
    public void Add_ShoppingCart_Item()
    {
        var repository = new ShoppingCartRepository();

        repository.Raise(new CreatedShoppingCartEvent(1, "Shopping Cart 1"));
        repository.Raise(new AddedShoppingCartItemEvent(1, "Item 1", 1));

        var shoppingCart = repository.Project(1).Result;
        var events = repository.Events();

        Assert.Equal("Shopping Cart 1", shoppingCart.Name);
        Assert.Single(shoppingCart.Items);
        Assert.Equal("Item 1", shoppingCart.Items.First().Name);
        Assert.Equal(1, shoppingCart.Items.First().Quantity);
        Assert.Equal(2, events.Count());
    }
}
