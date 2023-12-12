module SpecialOffers.API.Offers

open System
open Microsoft.AspNetCore.Http

open Giraffe

type Offer = { Id: int; Description: string }

type OfferStore =
    abstract member Get: int -> Option<Offer>
    abstract member Add: Offer -> Offer

type InMemoryOfferStore() =
    let mutable counter = 0
    let mutable offers = Map.empty<int, Offer>

    member this.Get(id: int) =
        Console.WriteLine(">>>GET")
        Console.WriteLine(offers)
        Console.WriteLine(">>>GET")
        Map.tryFind id offers

    member this.Add(offer: Offer) =
        counter <- counter + 1
        let newOffer = { offer with Id = counter }
        offers <- Map.add newOffer.Id newOffer offers
        newOffer

    interface OfferStore with
        member this.Get(id: int) = this.Get(id)
        member this.Add(offer: Offer) = this.Add(offer)

let getOfferHandler (id: int) : HttpHandler =
    (fun (next: HttpFunc) (ctx: HttpContext) ->
        let offerstore = ctx.GetService<OfferStore>()

        (match offerstore.Get id with
         | Some offer -> Successful.OK offer next ctx
         | None -> RequestErrors.NOT_FOUND "" next ctx))

let addOfferHandler: HttpHandler =
    (fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! newOffer = ctx.BindJsonAsync<Offer>()
            let offers = ctx.GetService<OfferStore>()
            let newOffer = offers.Add(newOffer)
            return! Successful.CREATED newOffer next ctx
        })


let routes: HttpHandler =
    choose
        [ route "/specialoffers" >=> POST >=> addOfferHandler
          routef "/specialoffers/%i" getOfferHandler ]
