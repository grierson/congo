using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var RegisteredUsers = new Dictionary<int, LoyaltyProgramUser>();

CreatedUserResponse MakeUser(LoyaltyProgramUser body)
{
    RegisteredUsers.Add(body.id, body);
    return new CreatedUserResponse(body.name);
}

app.MapGet("/users/{id:int}", (int id) =>
{
    if (RegisteredUsers.ContainsKey(id))
    {
        return Results.Ok(RegisteredUsers[id]);
    }

    return Results.NotFound();
});

app.MapPost("/users", (LoyaltyProgramUser body) =>
{
    var validator = new PostUserRequestValidator();
    var result = validator.Validate(body);

    if (!result.IsValid)
        return Results.BadRequest();

    var newUser = MakeUser(body);

    return Results.Created<CreatedUserResponse>($"/user/{body.id}", newUser);
});

app.MapPut("/users/{id:int}", (int id, LoyaltyProgramUser body) =>
{
    if (RegisteredUsers.ContainsKey(id))
    {
        RegisteredUsers[id] = body;
        return Results.Ok(body);
    }

    return Results.BadRequest();
});


app.Run();


public record CreatedUserResponse(string name);

public record Settings(string[] interests);

public record LoyaltyProgramUser(int id, string name, int loyaltyPoints, Settings settings);


public class PostUserRequestValidator : AbstractValidator<LoyaltyProgramUser>
{
    public PostUserRequestValidator()
    {
        RuleFor(request => request.id).NotEmpty().WithMessage("Please specify an id");
        RuleFor(request => request.name).NotEmpty().WithMessage("Please specify a name");
        RuleFor(request => request.loyaltyPoints).GreaterThanOrEqualTo(0).WithMessage("Please specify loyalty points");
    }
}

public partial class Program { }
