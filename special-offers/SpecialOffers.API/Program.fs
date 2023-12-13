module SpecialOffers.API.App

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe

open SpecialOffers.API.Events
open SpecialOffers.API.Offers
open SpecialOffers.API.DateTimeService

let webApp =
    choose [ SpecialOffers.API.Offers.routes; SpecialOffers.API.Events.routes ]

let configureApp (app: IApplicationBuilder) = app.UseGiraffe(webApp)

let configureServices (services: IServiceCollection) =
    services.AddSingleton<OfferStore, InMemoryOfferStore>() |> ignore
    services.AddSingleton<EventStore, InMemoryEventStore>() |> ignore
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
            webHostBuilder
                .Configure(configureApp)
                .ConfigureServices(configureServices)
                .UseKestrel(fun options -> options.Listen(System.Net.IPAddress.Any, 80) |> ignore)
            |> ignore)
        .Build()
        .Run()

    0
