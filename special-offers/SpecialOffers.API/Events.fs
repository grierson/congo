module Events

open System
open DateTimeService
open System.Threading

type EventFeedEvent =
    { SequenceNumber: int
      OccurredAt: DateTimeOffset
      Name: string
      Content: obj }

let mutable currentSequenceNumber = 0
let seqNumber = Interlocked.Increment(&currentSequenceNumber)

let addEvent
    (eventStore: EventFeedEvent list)
    (datetimeservice: IDateTimeService)
    (seqNumber: int)
    (name: string)
    (content: obj)
    =
    let now = datetimeservice.Now()

    let newEvent =
        { SequenceNumber = seqNumber
          OccurredAt = now
          Name = name
          Content = content }

    newEvent :: eventStore

let getEvents (database: EventFeedEvent list) (startRange: int) (endRange: int) : EventFeedEvent list =
    database
    |> List.filter (fun event -> event.SequenceNumber >= startRange && event.SequenceNumber <= endRange)
    |> List.sortBy _.SequenceNumber
