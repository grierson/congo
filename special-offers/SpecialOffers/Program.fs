module SpecialOffers

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe

open Events
open Offers
open DateTimeService

let webApp = choose [ Offers.routes; Events.routes ]

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
                .UseKestrel(fun options -> options.Listen(System.Net.IPAddress.Any, 8080) |> ignore)
            |> ignore)
        .Build()
        .Run()

    0
