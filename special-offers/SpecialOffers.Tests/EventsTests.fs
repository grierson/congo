module EventsTests

open Program
open Xunit
open Swensen.Unquote
open System.Net
open Microsoft.AspNetCore.Mvc.Testing

let runTestApi () =
    (new WebApplicationFactory<Program>()).Server


(* [<Fact>] *)
(* let ``offer not found`` () = *)
(*     task { *)
(*         let client = runTestApi().CreateClient() *)
(*         let! response = client.GetAsync("/events") *)
(**)
(*         test <@ HttpStatusCode.OK = response.StatusCode @> *)
(*     } *)
