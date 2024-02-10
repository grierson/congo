namespace Application.Tests;

public class UnitTest1
{
    [Fact]
    public void Create_ShoppingCart_Test()
    {
        var @event = new CreatedShoppingCartEvent("Test");
        var cart = ShoppingCart.Create(@event);
        Assert.Equal("Test", cart.Name);
    }

    [Fact]
    public void Add_Item_To_ShoppingCart_Test()
    {
        var @create_event = new CreatedShoppingCartEvent("Test");
        var cart = ShoppingCart.Create(@create_event);

        const string product_name = "Item";
        const int product_quantity = 1;
        var add_item_event =
            new AddedShoppingCartItemEvent(product_name, product_quantity);

        var new_cart = cart.Apply(add_item_event);

        Assert.Single(new_cart.Items);
        Assert.Equal("Item", new_cart.Items.First().Name);
        Assert.Equal(1, new_cart.Items.First().Quantity);
    }
}
