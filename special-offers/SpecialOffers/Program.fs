module SpecialOffers

open System
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe

open Events
open Offers
open DateTimeService

let webApp = choose [ Offers.routes; Events.routes ]

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> ServerErrors.INTERNAL_ERROR ex.Message

let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole().AddDebug()

    |> ignore

let configureApp (app: IApplicationBuilder) =
    app.UseGiraffeErrorHandler(errorHandler).UseGiraffe(webApp)

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
                .ConfigureLogging(configureLogging)
                .UseKestrel(fun options -> options.Listen(System.Net.IPAddress.Any, 80) |> ignore)

            |> ignore)
        .Build()
        .Run()

    0
