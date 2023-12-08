module Tests

open Program
open Xunit
open Swensen.Unquote
open Microsoft.AspNetCore.Mvc.Testing

let runTestApi () =
    (new WebApplicationFactory<Program>()).Server

[<Fact>]
let ``Return hello world`` () = Assert.True(true)

[<Fact>]
let ``demo Unquote xUnit support`` () = test <@ 2 = 2 @>


[<Fact>]
let ``returns hello world`` () =
    task {
        let client = runTestApi().CreateClient()
        let! response = client.GetAsync("/hello")
        let! result = response.Content.ReadAsStringAsync()

        test <@ "world" = result @>
    }
