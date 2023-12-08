namespace LoyaltyProgram.Tests;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;


public class UsersTests
{
    [Fact]
    public async Task create_valid_user()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var name = "Alice";
        const int id = 4;

        LoyaltyProgramUser body = new LoyaltyProgramUser(id, name, 0, new Settings(["whisky", "cycling"]));

        var result = await client.PostAsJsonAsync("/users", body);
        var actual = await result.Content.ReadFromJsonAsync<CreatedUserResponse>();

        var expected = new CreatedUserResponse(name);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Equal($"/users/{id}", result.Headers.Location.ToString());
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task create_same_user_twice_fails()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        const int id = 4;

        LoyaltyProgramUser body = new LoyaltyProgramUser(id, "alice", 0, new Settings(["whisky", "cycling"]));

        await client.PostAsJsonAsync("/users", body);

        var result = await client.PostAsJsonAsync("/users", body);
        var actual = await result.Content.ReadFromJsonAsync<string>();

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("User already exists", actual);
    }

    [Fact]
    public async Task update_valid_user()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var id = 4;
        var name = "Alice";
        var new_name = "Bob";

        var post_body = new LoyaltyProgramUser(id, name, 0, new Settings(["whisky", "cycling"]));
        await client.PostAsJsonAsync("/users", post_body);

        var put_body = new LoyaltyProgramUser(id, new_name, 0, new Settings(["whisky", "cycling"]));
        var result = await client.PutAsJsonAsync($"/users/{id}", put_body);
        var actual = await result.Content.ReadFromJsonAsync<CreatedUserResponse>();

        var expected = new CreatedUserResponse(new_name);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task get_valid_user()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        const int id = 1;

        var body = new LoyaltyProgramUser(id, "Alice", 0, new Settings(["whisky", "cycling"]));
        await client.PostAsJsonAsync("/users", body);

        var response = await client.GetAsync($"/users/{id}");
        var result = await response.Content.ReadFromJsonAsync<LoyaltyProgramUser>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(body.id, result.id);
        Assert.Equal(body.name, result.name);
        Assert.Equal(body.loyaltyPoints, result.loyaltyPoints);
        Assert.Equal(body.settings.interests, result.settings.interests);
    }

    [Fact]
    public async Task unable_to_find_user()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync($"/users/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
