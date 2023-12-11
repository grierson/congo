module SpecialOfferTests

open Program
open System
open Xunit
open Swensen.Unquote
open System.Net
open System.Net.Http.Json
open System.Collections.Generic
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Hosting

let makeWebApplication () = (new WebApplicationFactory<Program>())

let setup date =
    let dateTimeService =
        { new IDateTimeService with
            member this.Now() = date }

    let configureServices (services: IServiceCollection) =
        services.AddScoped<IDateTimeService>(fun _ -> dateTimeService) |> ignore

        ()

    let webhostBuilder (builder: IWebHostBuilder) =
        builder.ConfigureServices(Action<IServiceCollection>(configureServices))
        |> ignore

        ()

    (new WebApplicationFactory<Program>()).WithWebHostBuilder(webhostBuilder)



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

        let! response = client.GetAsync("/events?startRange=1&endRange=10")

        let! content = response.Content.ReadFromJsonAsync<EventFeedEvent list>()

        test <@ HttpStatusCode.OK = response.StatusCode @>
        test <@ List.isEmpty content @>
    }

[<Fact>]
let ``Get first Special Offer created event`` () =
    task {
        let date = DateTimeOffset(DateTime(year = 2023, month = 1, day = 1))

        let application = setup date

        let client = application.CreateClient()

        let createSpecialOfferRequest = { Id = 1; Description = "New thing" }
        let! _ = client.PostAsJsonAsync<Offer>("specialoffers/", createSpecialOfferRequest)

        let! response = client.GetAsync("/events?startRange=1&endRange=10")
        let! content = response.Content.ReadFromJsonAsync<List<EventFeedEvent>>()
        let event = content[0]

        test <@ HttpStatusCode.OK = response.StatusCode @>
        test <@ 1 = event.SequenceNumber @>
        test <@ "SpecialOfferCreated" = event.Name @>
        test <@ date = event.OccurredAt @>
    }
