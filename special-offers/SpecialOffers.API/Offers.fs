module SpecialOffers.API.Offers

open Microsoft.AspNetCore.Http
open Giraffe

open SpecialOffers.API.Events

type Offer = { Id: int; Description: string }

type OfferStore =
    abstract member Get: int -> Option<Offer>
    abstract member Add: Offer -> Offer
    abstract member Update: Offer -> Offer
    abstract member Delete: int -> unit

type InMemoryOfferStore() =
    let mutable counter = 0
    let mutable offers = Map.empty<int, Offer>

    member this.Get(id: int) = Map.tryFind id offers

    member this.Add(offer: Offer) =
        counter <- counter + 1
        let newOffer = { offer with Id = counter }
        offers <- Map.add newOffer.Id newOffer offers
        newOffer

    member this.Update(offer: Offer) =
        offers <- Map.add offer.Id offer offers
        offer

    member this.Delete(id: int) = offers <- Map.remove id offers

    interface OfferStore with
        member this.Get(id: int) = this.Get(id)
        member this.Add(offer: Offer) = this.Add(offer)
        member this.Update(offer: Offer) = this.Update(offer)
        member this.Delete(id: int) = this.Delete(id)

let getOfferHandler (id: int) : HttpHandler =
    (fun (next: HttpFunc) (ctx: HttpContext) ->
        let offerstore = ctx.GetService<OfferStore>()

        (match offerstore.Get id with
         | Some offer -> Successful.OK offer next ctx
         | None -> RequestErrors.NOT_FOUND "" next ctx))

let addOfferHandler: HttpHandler =
    (fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! offer = ctx.BindJsonAsync<Offer>()
            let offerStore = ctx.GetService<OfferStore>()
            let eventStore = ctx.GetService<EventStore>()
            eventStore.RaiseEvent "SpecialOfferCreated" offer
            let newOffer = offerStore.Add(offer)

            return! Successful.CREATED newOffer next ctx
        })


let updateOfferHandler (id: int) : HttpHandler =
    (fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! updated_offer = ctx.BindJsonAsync<Offer>()
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
