module SpecialOffers.API.App

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open System.Collections.Generic

open Offers

let webApp = Offers.routes

let configureApp (app: IApplicationBuilder) = app.UseGiraffe(webApp)

let configureServices (services: IServiceCollection) =
    let mutable offerStore = Dictionary<int, Offer>()
    services.AddSingleton<IDictionary<int, Offer>>(offerStore) |> ignore
    services.AddGiraffe() |> ignore

type Program() =
    class
    end

[<EntryPoint>]
let main _ =
    Host
        .CreateDefaultBuilder()
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder.Configure(configureApp).ConfigureServices(configureServices)
            |> ignore)
        .Build()
        .Run()

    0
