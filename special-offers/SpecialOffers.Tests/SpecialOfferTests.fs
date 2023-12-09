module SpecialOfferTests

open Program
open Xunit
open Swensen.Unquote
open System.Net
open System.Net.Http.Json
open Microsoft.AspNetCore.Mvc.Testing

let runTestApi () =
    (new WebApplicationFactory<Program>()).Server


[<Fact>]
let ``offer not found`` () =
    task {
        let client = runTestApi().CreateClient()
        let! response = client.GetAsync("specialoffers/1")

        test <@ HttpStatusCode.NotFound = response.StatusCode @>
    }


[<Fact>]
let ``offer found`` () =
    task {
        let client = runTestApi().CreateClient()

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
        let client = runTestApi().CreateClient()

        let request = { Id = 1; Description = "New thing" }
        let! response = client.PostAsJsonAsync<Offer>("specialoffers/", request)
        let! content = response.Content.ReadFromJsonAsync<Offer>()

        test <@ HttpStatusCode.Created = response.StatusCode @>
        test <@ request = content @>
    }


[<Fact>]
let ``update offer`` () =
    task {
        let client = runTestApi().CreateClient()

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
        let client = runTestApi().CreateClient()

        let! response = client.DeleteAsync($"specialoffers/1")

        test <@ HttpStatusCode.NotFound = response.StatusCode @>
    }

[<Fact>]
let ``delete offer`` () =
    task {
        let client = runTestApi().CreateClient()

        let id = 1

        let request = { Id = id; Description = "New thing" }
        let! _ = client.PostAsJsonAsync<Offer>("specialoffers/", request)

        let! delete_response = client.DeleteAsync($"specialoffers/{1}")

        let! get_response = client.GetAsync("specialoffers/1")

        test <@ HttpStatusCode.OK = delete_response.StatusCode @>
        test <@ HttpStatusCode.NotFound = get_response.StatusCode @>
    }
