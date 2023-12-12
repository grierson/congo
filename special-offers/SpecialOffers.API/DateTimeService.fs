module DateTimeService

open System

type IDateTimeService =
    abstract member Now: unit -> DateTimeOffset

type DateTimeService() =
    interface IDateTimeService with
        member this.Now() = DateTimeOffset.UtcNow
