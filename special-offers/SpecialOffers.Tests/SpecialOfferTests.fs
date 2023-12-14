module SpecialOfferTests

open SpecialOffers
open Offers
open DateTimeService
open Events

open System
open Xunit
open Swensen.Unquote
open System.Net
open System.Net.Http.Json
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Hosting

let makeWebApplication () = (new WebApplicationFactory<Program>())

let setup date =
    let dateTimeService =
        { new DateTimeService with
            member this.Now() = date }

    let configureServices (services: IServiceCollection) =
        services.AddScoped<DateTimeService>(fun _ -> dateTimeService) |> ignore
        services.AddSingleton<EventStore, InMemoryEventStore>() |> ignore
        services.AddSingleton<OfferStore, InMemoryOfferStore>() |> ignore

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
        let! response = client.GetAsync("/specialoffers/1")

        test <@ HttpStatusCode.NotFound = response.StatusCode @>
    }


[<Fact>]
let ``offer found`` () =
    task {
        let application = setup DateTimeOffset.Now
        let client = application.CreateClient()

        let id = 1

        let request = { Id = id; Description = "New thing" }
        let! createOfferResponse = client.PostAsJsonAsync<Offer>("/specialoffers", request)

        let! createOfferResponseContent = createOfferResponse.Content.ReadFromJsonAsync<Offer>()

        let! getOfferResponse = client.GetAsync($"/specialoffers/{createOfferResponseContent.Id}")

        let! getOfferResponseContent = getOfferResponse.Content.ReadFromJsonAsync<Offer>()

        test <@ HttpStatusCode.OK = getOfferResponse.StatusCode @>
        test <@ request = getOfferResponseContent @>
    }


[<Fact>]
let ``create offer`` () =
    task {
        let date = DateTimeOffset(DateTime(year = 2023, month = 1, day = 1))

        let application = setup date

        let client = application.CreateClient()

        let request = { Id = 1; Description = "New thing" }
        let! create_offer_response = client.PostAsJsonAsync<Offer>("/specialoffers", request)
        let! create_offer_response_content = create_offer_response.Content.ReadFromJsonAsync<Offer>()

        let! events_response = client.GetAsync("/events?startRange=1&endRange=10")
        let! events_response_content = events_response.Content.ReadFromJsonAsync<List<EventFeedEvent>>()
        let event = events_response_content[0]

        test <@ HttpStatusCode.Created = create_offer_response.StatusCode @>
        test <@ request = create_offer_response_content @>

        test <@ HttpStatusCode.OK = events_response.StatusCode @>
        test <@ 1 = event.SequenceNumber @>
        test <@ "SpecialOfferCreated" = event.Name @>
        test <@ date = event.OccurredAt @>
    }

[<Fact>]
let ``update offer`` () =
    task {
        let date = DateTimeOffset(DateTime(year = 2023, month = 1, day = 1))

        let application = setup date

        let client = application.CreateClient()

        let id = 1

        let create_offer_request = { Id = id; Description = "New thing" }
        let! create_offer_response = client.PostAsJsonAsync<Offer>("/specialoffers", create_offer_request)

        let updated_request =
            { create_offer_request with
                Description = "Other thing" }

        let! updated_offer_response = client.PutAsJsonAsync($"/specialoffers/{id}", updated_request)
        let! updated_offer_response_content = updated_offer_response.Content.ReadFromJsonAsync<Offer>()

        let! events_response = client.GetAsync("/events?startRange=1&endRange=10")
        let! events_response_content = events_response.Content.ReadFromJsonAsync<List<EventFeedEvent>>()
        let event = events_response_content[1]

        test <@ HttpStatusCode.OK = updated_offer_response.StatusCode @>
        test <@ updated_request = updated_offer_response_content @>

        test <@ HttpStatusCode.OK = events_response.StatusCode @>
        test <@ 2 = event.SequenceNumber @>
        test <@ "SpecialOfferUpdated" = event.Name @>
        test <@ date = event.OccurredAt @>
    }

[<Fact>]
let ``delete not found`` () =
    task {
        let client = makeWebApplication().CreateClient()

        let! response = client.DeleteAsync($"/specialoffers/1")

        test <@ HttpStatusCode.NotFound = response.StatusCode @>
    }

[<Fact>]
let ``delete offer`` () =
    task {
        let date = DateTimeOffset(DateTime(year = 2023, month = 1, day = 1))

        let application = setup date

        let client = application.CreateClient()

        let id = 1
        let request = { Id = id; Description = "New thing" }

        let! _ = client.PostAsJsonAsync<Offer>("/specialoffers", request)
        let! delete_offer_response = client.DeleteAsync($"/specialoffers/{id}")
        let! get_offer_response = client.GetAsync($"/specialoffers/{id}")

        let! events_response = client.GetAsync("/events?startRange=1&endRange=10")
        let! events_response_content = events_response.Content.ReadFromJsonAsync<List<EventFeedEvent>>()
        let event = events_response_content[1]

        test <@ HttpStatusCode.OK = delete_offer_response.StatusCode @>
        test <@ HttpStatusCode.NotFound = get_offer_response.StatusCode @>

        test <@ HttpStatusCode.OK = events_response.StatusCode @>
        test <@ 2 = event.SequenceNumber @>
        test <@ "SpecialOfferDeleted" = event.Name @>
        test <@ date = event.OccurredAt @>
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
