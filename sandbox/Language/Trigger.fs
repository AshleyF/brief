module Trigger

open System
open System.Net.Http
open Structure
open Primitives
open Actor

let triggerActor =
    let triggerState =
        let mutable (primitives : Map<string, (State -> State)>) = primitiveState.Primitives
        let primitive name fn = primitives <- Map.add name fn primitives

        let triggerEvent event val1 val2 val3 key = async {
            use client = new HttpClient(Timeout = TimeSpan.FromMinutes 1.)
            client.Timeout <- TimeSpan.FromMinutes 1.
            let url = sprintf "https://maker.ifttt.com/trigger/%s/with/key/%s?value1=%s&value2=%s&value3=%s" event key val1 val2 val3
            use! response = Async.AwaitTask (client.GetAsync(url))
            response.EnsureSuccessStatusCode() |> ignore }

        primitive "hook" (fun s ->
            match s.Stack with
            | String key :: String event :: String val1 :: String val2 :: String val3 :: t ->
                triggerEvent event val1 val2 val3 key |> Async.RunSynchronously
                { s with Stack = t }
            | _ :: _ :: _ :: _ :: _ :: _ -> failwith "Expected ssss"
            | _ -> failwith "Stack underflow")

        { primitiveState with Primitives = primitives }

    actor triggerState
