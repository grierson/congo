module SpecialOfferTests

open Program
open System
open Xunit
open Swensen.Unquote
open System.Net
open System.Net.Http
open System.Net.Http.Json
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.TestHost

let makeWebApplication () = (new WebApplicationFactory<Program>())

[<Fact>]
let ``offer not found`` () =
    task {
        let client = makeWebApplication().CreateClient()
        let! response = client.GetAsync("specialoffers/1")

        test <@ HttpStatusCode.NotFound = response.StatusCode @>
    }

[<Fact>]
let ``offer found`` () =
    task {
        let client = makeWebApplication().CreateClient()

        let id = 1

        let request = { Id = id; Description = "New thing" }
        let! _ = client.PostAsJsonAsync<Offer>("specialoffers/", request)

        let! response = client.GetAsync($"specialoffers/{id}")
        let! content = response.Content.ReadFromJsonAsync<Offer>()

        test <@ HttpStatusCode.OK = response.StatusCode @>
        test <@ request = content @>
    }

[<Fact>]
let ``create offer`` () =
    task {
        let client = makeWebApplication().CreateClient()

        let request = { Id = 1; Description = "New thing" }
        let! response = client.PostAsJsonAsync<Offer>("specialoffers/", request)
        let! content = response.Content.ReadFromJsonAsync<Offer>()

        test <@ HttpStatusCode.Created = response.StatusCode @>
        test <@ request = content @>
    }


[<Fact>]
let ``update offer`` () =
    task {
        let client = makeWebApplication().CreateClient()

        let id = 1

        let request = { Id = id; Description = "New thing" }
        let! _ = client.PostAsJsonAsync<Offer>("specialoffers/", request)

        let updated_request =
            { request with
                Description = "Other thing" }

        let! response = client.PutAsJsonAsync($"specialoffers/{id}", updated_request)
        let! content = response.Content.ReadFromJsonAsync<Offer>()

        test <@ HttpStatusCode.OK = response.StatusCode @>
        test <@ updated_request = content @>
    }


[<Fact>]
let ``delete not found`` () =
    task {
        let client = makeWebApplication().CreateClient()

        let! response = client.DeleteAsync($"specialoffers/1")

        test <@ HttpStatusCode.NotFound = response.StatusCode @>
    }

[<Fact>]
let ``delete offer`` () =
    task {
        let client = makeWebApplication().CreateClient()

        let id = 1
        let request = { Id = id; Description = "New thing" }

        let! _ = client.PostAsJsonAsync<Offer>("specialoffers/", request)
        let! delete_response = client.DeleteAsync($"specialoffers/{id}")
        let! get_response = client.GetAsync($"specialoffers/{id}")

        test <@ HttpStatusCode.OK = delete_response.StatusCode @>
        test <@ HttpStatusCode.NotFound = get_response.StatusCode @>
    }


[<Fact>]
let ``Get empty events`` () =
    task {
        let client = makeWebApplication().CreateClient()

        let! response = client.GetAsync("/events?start=1")

        let! content = response.Content.ReadFromJsonAsync<EventFeedEvent list>()

        test <@ HttpStatusCode.OK = response.StatusCode @>
        test <@ List.isEmpty content @>
    }

type TestDateTimeService =
    interface IDateTimeService with
        member this.Now() = DateTimeOffset.Now.AddDays(10)


type TestFooService =
    interface IFooService with
        member this.foo() = "test foo service"

let configureServices (services: IServiceCollection) =
    services.AddScoped<IFooService, TestFooService>() |> ignore

let make_builder =
    Action<IWebHostBuilder>(fun builder ->
        builder.ConfigureServices(Action<IServiceCollection>(fun services -> configureServices services))
        |> ignore

        ())

[<Fact>]
let ``Foo service test`` () =
    task {
        let application = (new WebApplicationFactory<Program>())

        Console.WriteLine("Services")
        Console.WriteLine(application.Services.GetServices)
        Console.WriteLine("Services")

        let builder = make_builder
        application.WithWebHostBuilder(builder) |> ignore
        let client = application.CreateClient()

        let! response = client.GetAsync("/events/foo")
        let! content = response.Content.ReadAsStringAsync()

        test <@ HttpStatusCode.OK = response.StatusCode @>
        test <@ "Test Foo" = content @>
    }
