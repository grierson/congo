module SpecialOffers.API.DateTimeService

open System

type DateTimeService =
    abstract member Now: unit -> DateTimeOffset

type NowDateTimeService() =
    interface DateTimeService with
        member this.Now() = DateTimeOffset.UtcNow
