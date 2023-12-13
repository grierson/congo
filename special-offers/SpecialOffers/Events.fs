module Events

open System
open DateTimeService
open Microsoft.AspNetCore.Http
open Giraffe


type EventFeedEvent =
    { SequenceNumber: int
      OccurredAt: DateTimeOffset
      Name: string
      Content: obj }

type EventStore =
    abstract member Get: int -> int -> EventFeedEvent list
    abstract member RaiseEvent: string -> obj -> unit

type InMemoryEventStore(datetimeservice: DateTimeService) =
    let mutable counter = 1
    let mutable events = []

    interface EventStore with
        member this.Get (startRange: int) (endRange: int) =
            events
            |> List.filter (fun event -> event.SequenceNumber >= startRange && event.SequenceNumber <= endRange)
            |> List.sortBy _.SequenceNumber
            |> List.filter (fun event -> event.SequenceNumber >= startRange && event.SequenceNumber <= endRange)
            |> List.sortBy _.SequenceNumber

        member this.RaiseEvent (name: string) (content: obj) =
            let now = datetimeservice.Now()

            let event =
                { OccurredAt = now
                  SequenceNumber = counter
                  Name = name
                  Content = content }

            counter <- counter + 1
            events <- event :: events


let tryGetIntValue (ctx: HttpContext) (key: string) =
    match ctx.TryGetQueryStringValue(key) with
    | Some value ->
        match System.Int32.TryParse(value) with
        | (true, result) -> Some result
        | _ -> None
    | None -> None

let getHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        match tryGetIntValue ctx "startRange", tryGetIntValue ctx "endRange" with
        | Some startRange, Some endRange ->
            if (startRange < 0 || endRange < startRange) then
                RequestErrors.BAD_REQUEST "Invalid query parameters" next ctx
            else
                let eventstore = ctx.GetService<EventStore>()
                let events = eventstore.Get startRange endRange
                Successful.OK events next ctx
        | _ -> RequestErrors.BAD_REQUEST "Invalid query parameters" next ctx


let routes: HttpHandler = route "/events" >=> getHandler
