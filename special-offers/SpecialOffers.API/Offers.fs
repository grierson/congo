module SpecialOffers.API.Offers

open System
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open System.Collections.Generic

open Giraffe

type Offer = { Id: int; Description: string }

let getOfferHandler (id: int) : HttpHandler =
    (fun (next: HttpFunc) (ctx: HttpContext) ->

        let offers =
            ctx.RequestServices.GetService(typeof<IDictionary<int, Offer>>) :?> IDictionary<int, Offer>

        (match offers.TryGetValue id with
         | (true, offer) -> Successful.OK offer next ctx
         | (false, _) -> RequestErrors.NOT_FOUND "" next ctx))

let addOfferHandler: HttpHandler =
    (fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! newOffer = ctx.BindJsonAsync<Offer>()

            let mutable offers =
                ctx.RequestServices.GetService(typeof<IDictionary<int, Offer>>) :?> IDictionary<int, Offer>

            let id = 1
            let newOffer = { newOffer with Id = id }
            offers.Add(id, newOffer)
            return! Successful.CREATED newOffer next ctx
        })


let routes: HttpHandler =
    choose
        [ route "/specialoffers" >=> POST >=> addOfferHandler
          routef "/specialoffers/%i" getOfferHandler ]
