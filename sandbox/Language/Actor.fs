module Actor

open Structure
open Syntax
open Interpretation

type BriefActor = MailboxProcessor<string>

let actor state : BriefActor =
    BriefActor.Start((fun channel ->
        let rec loop state = async {
            let! input = channel.Receive()
            printfn "INPUT %s" input
            try return! input |> brief state.Dictionary |> interpret state false |> loop
            with ex -> printfn "Actor Error: %s" ex.Message }
        loop state))

let mutable (registry: Map<string, BriefActor>) = Map.empty

let register name actor = registry <- Map.add name actor registry
