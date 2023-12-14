module Offers

open Microsoft.AspNetCore.Http
open System.Text.Json
open Giraffe

open Events

[<CLIMutable>]
type Offer = { Id: int; Description: string }

type OfferStore =
    abstract member Get: int -> Option<Offer>
    abstract member Add: Offer -> Offer
    abstract member Update: Offer -> Offer
    abstract member Delete: int -> unit

type InMemoryOfferStore() =
    let mutable counter = 1
    let mutable offers = Map.empty<int, Offer>

    interface OfferStore with
        member this.Get(id: int) = Map.tryFind id offers

        member this.Add(offer: Offer) =
            let newOffer = { offer with Id = counter }
            offers <- Map.add newOffer.Id newOffer offers
            counter <- counter + 1
            newOffer

        member this.Update(offer: Offer) =
            offers <- Map.add offer.Id offer offers
            offer

        member this.Delete(id: int) = offers <- Map.remove id offers


let getOfferHandler (id: int) : HttpHandler =
    (fun (next: HttpFunc) (ctx: HttpContext) ->
        let offerstore = ctx.GetService<OfferStore>()

        (match offerstore.Get id with
         | Some offer -> Successful.OK offer next ctx
         | None -> RequestErrors.NOT_FOUND "" next ctx))

let addOfferHandler: HttpHandler =
    (fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let offerStore = ctx.GetService<OfferStore>()
            let eventStore = ctx.GetService<EventStore>()

            let! body = ctx.ReadBodyFromRequestAsync()
            let offer = JsonSerializer.Deserialize<Offer>(body)

            eventStore.RaiseEvent "SpecialOfferCreated" offer
            let newOffer = offerStore.Add(offer)

            return! Successful.CREATED newOffer next ctx
        })

let updateOfferHandler (id: int) : HttpHandler =
    (fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! body = ctx.ReadBodyFromRequestAsync()
            let updated_offer = JsonSerializer.Deserialize<Offer>(body)
            let offerStore = ctx.GetService<OfferStore>()
            let eventStore = ctx.GetService<EventStore>()

            match offerStore.Get id with
            | Some _ ->
                let newOffer = offerStore.Update updated_offer
                eventStore.RaiseEvent "SpecialOfferUpdated" newOffer
                return! Successful.OK newOffer next ctx
            | None -> return! RequestErrors.NOT_FOUND "" next ctx
        })


let deleteOfferHandler (id: int) : HttpHandler =
    (fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let offerStore = ctx.GetService<OfferStore>()
            let eventStore = ctx.GetService<EventStore>()

            match offerStore.Get id with
            | Some _ ->
                offerStore.Delete id
                eventStore.RaiseEvent "SpecialOfferDeleted" id
                return! Successful.OK "" next ctx
            | None -> return! RequestErrors.NOT_FOUND "" next ctx
        })


let routes: HttpHandler =
    choose
        [ route "/specialoffers" >=> POST >=> addOfferHandler
          routef "/specialoffers/%i" (fun id ->
              choose
                  [ GET >=> getOfferHandler id
                    PUT >=> updateOfferHandler id
                    DELETE >=> deleteOfferHandler id ]) ]
