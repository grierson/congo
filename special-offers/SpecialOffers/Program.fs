open FSharp.MinimalApi.Builder
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.HttpResults
open System.Collections.Generic
open type TypedResults

type Offer = { Id: int; Description: string }

let mutable offers = Dictionary<int, Offer>()

let routes =
    endpoints {
        routeGroup "specialoffers" {
            get "{id:int}" produces<Ok<Offer>, NotFound> (fun (req: {| id: int |}) ->
                (match offers.TryGetValue(req.id) with
                 | true, offer -> !! Ok(offer)
                 | false, _ -> !! NotFound()))

            post "" produces<Created<Offer>, Conflict> (fun (req: {| offer: Offer |}) ->
                (match offers.ContainsKey(req.offer.Id) with
                 | true -> !! Conflict()
                 | false ->
                     offers.Add(req.offer.Id, req.offer)
                     !! Created($"/{req.offer.Id}/", req.offer)))

            delete "{id:int}" produces<Ok, NotFound> (fun (req: {| id: int |}) ->
                (match offers.TryGetValue(req.id) with
                 | true, _ ->
                     offers.Remove(req.id) |> ignore
                     !! Ok()
                 | false, _ -> !! NotFound()))

            put "{id:int}" produces<Ok<Offer>, NotFound> (fun (req: {| offer: Offer |}) ->
                (match offers.TryGetValue(req.offer.Id) with
                 | true, _ ->
                     offers[req.offer.Id] <- req.offer
                     !! Ok(req.offer)
                 | false, _ -> !! NotFound()))
        }

        routeGroup "events" { get "" (fun (_) -> Ok()) }
    }

let app = WebApplication.CreateBuilder().Build()
routes.Apply app |> ignore
app.Run()

type Program() =
    class
    end
