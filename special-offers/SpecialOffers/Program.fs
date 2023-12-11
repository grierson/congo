open System
open System.Collections.Generic
open System.Threading
open FSharp.MinimalApi.Builder
open FSharp.MinimalApi
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.HttpResults
open Microsoft.Extensions.DependencyInjection
open type TypedResults

type Offer = { Id: int; Description: string }

type EventFeedEvent =
    { SequenceNumber: int
      OccurredAt: DateTimeOffset
      Name: string
      Content: obj }

type IDateTimeService =
    abstract member Now: unit -> DateTimeOffset

type DateTimeService() =
    interface IDateTimeService with
        member this.Now() = DateTimeOffset.UtcNow

let mutable currentSequenceNumber = 0
let mutable database = List.Empty

let raiseEvent (datetimeservice: IDateTimeService) (name: string) (content: obj) =
    let seqNumber = Interlocked.Increment(&currentSequenceNumber)
    let now = datetimeservice.Now()

    let newEvent =
        { SequenceNumber = seqNumber
          OccurredAt = now
          Name = name
          Content = content }

    database <- newEvent :: database

let getEvents (database: EventFeedEvent list) (startRange: int) (endRange: int) : EventFeedEvent list =
    database
    |> List.filter (fun event -> event.SequenceNumber >= startRange && event.SequenceNumber <= endRange)
    |> List.sortBy _.SequenceNumber


let offers = Dictionary<int, Offer>()

let routes =
    endpoints {
        routeGroup "specialoffers" {
            get "{id:int}" produces<Ok<Offer>, NotFound> (fun (req: {| id: int |}) ->
                (match offers.TryGetValue(req.id) with
                 | true, offer -> !! Ok(offer)
                 | false, _ -> !! NotFound()))

            post
                ""
                produces<Created<Offer>, Conflict>
                (fun
                    (req:
                        {| offer: Offer
                           datetimeservice: IDateTimeService |}) ->
                    (match offers.ContainsKey(req.offer.Id) with
                     | true -> !! Conflict()
                     | false ->
                         raiseEvent req.datetimeservice "SpecialOfferCreated" req.offer
                         offers.Add(req.offer.Id, req.offer)
                         !! Created($"/specialoffers/{req.offer.Id}/", req.offer)))

            delete
                "{id:int}"
                produces<Ok, NotFound>
                (fun
                    (req:
                        {| id: int
                           datetimeservice: IDateTimeService |}) ->
                    (match offers.TryGetValue(req.id) with
                     | true, _ ->
                         raiseEvent req.datetimeservice "SpecialOfferDeleted" req.id
                         offers.Remove(req.id) |> ignore
                         !! Ok()
                     | false, _ -> !! NotFound()))

            put
                "{id:int}"
                produces<Ok<Offer>, NotFound>
                (fun
                    (req:
                        {| offer: Offer
                           datetimeservice: IDateTimeService |}) ->
                    (match offers.TryGetValue(req.offer.Id) with
                     | true, _ ->
                         raiseEvent req.datetimeservice "SpecialOfferUpdated" req.offer
                         offers[req.offer.Id] <- req.offer
                         !! Ok(req.offer)
                     | false, _ -> !! NotFound()))
        }

        routeGroup "events" {
            get "" produces<Ok<EventFeedEvent list>, BadRequest> (fun (req: {| startRange: int; endRange: int |}) ->
                if (req.startRange < 0 || req.endRange < req.startRange) then
                    !! BadRequest()
                else
                    let events = getEvents database req.startRange req.endRange
                    !! Ok(events))
        }
    }

let builder = WebApplication.CreateBuilder()
builder.Services.AddScoped<IDateTimeService, DateTimeService>() |> ignore
let app = builder.Build()
routes.Apply app |> ignore
app.Run()

type Program() =
    class
    end
