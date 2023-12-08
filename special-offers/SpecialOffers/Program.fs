open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Builder

let routes =
    endpoints {
        get "/hello" (fun () -> "world")
        get "/ping/{x}" (fun (req: {| x: int |}) -> $"pong {req.x}")
    }

let app = WebApplication.CreateBuilder().Build()
routes.Apply app |> ignore
app.Run()

type Program() =
    class
    end
