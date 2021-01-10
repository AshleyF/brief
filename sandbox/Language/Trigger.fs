module Trigger

open System
open System.Net.Http
open Structure
open Primitives
open Actor

let triggerActor =

    let triggerEvent event val1 val2 val3 key = async {
        use client = new HttpClient(Timeout = TimeSpan.FromMinutes 1.)
        client.Timeout <- TimeSpan.FromMinutes 1.
        let url = sprintf "https://maker.ifttt.com/trigger/%s/with/key/%s?value1=%s&value2=%s&value3=%s" event key val1 val2 val3
        use! response = Async.AwaitTask (client.GetAsync(url))
        response.EnsureSuccessStatusCode() |> ignore }

    let triggerState =
        [
            primitive "hook" (fun s ->
                match getStack s with
                | String key :: String event :: String val1 :: String val2 :: String val3 :: t ->
                    triggerEvent event val1 val2 val3 key |> Async.RunSynchronously
                    setStack t s
                | _ :: _ :: _ :: _ :: _ :: _ -> failwith "Expected ssss"
                | _ -> failwith "Stack underflow")

        ] |> addPrimitives primitiveState

    actor triggerState
