module SpecialOffers.API.Events

open System
open SpecialOffers.API.DateTimeService
open System.Threading

type EventFeedEvent =
    { SequenceNumber: int
      OccurredAt: DateTimeOffset
      Name: string
      Content: obj }

type EventStore =
    abstract member RaiseEvent: string -> obj -> unit

type InMemoryEventStore(datetimeservice: DateTimeService) =
    let mutable counter = 0
    let mutable events = []

    interface EventStore with
        member this.RaiseEvent (name: string) (content: obj) =
            let now = datetimeservice.Now()

            let event =
                { OccurredAt = now
                  SequenceNumber = counter
                  Name = name
                  Content = content }

            counter <- counter + 1
            events <- event :: events

let getEvents (database: EventFeedEvent list) (startRange: int) (endRange: int) : EventFeedEvent list =
    database
    |> List.filter (fun event -> event.SequenceNumber >= startRange && event.SequenceNumber <= endRange)
    |> List.sortBy _.SequenceNumber
    |> List.filter (fun event -> event.SequenceNumber >= startRange && event.SequenceNumber <= endRange)
    |> List.sortBy _.SequenceNumber
