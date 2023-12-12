module SpecialOffers.API.App

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe

open System.Collections.Generic

open SpecialOffers.API.Offers
open SpecialOffers.API.Events
open SpecialOffers.API.DateTimeService

let webApp = Offers.routes

let configureApp (app: IApplicationBuilder) = app.UseGiraffe(webApp)

let configureServices (services: IServiceCollection) =
    services.AddScoped<OfferStore, InMemoryOfferStore>() |> ignore
    services.AddScoped<EventStore, InMemoryEventStore>() |> ignore
    services.AddScoped<DateTimeService, NowDateTimeService>() |> ignore
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
