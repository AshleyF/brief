module Actor

open Structure
open Syntax
open Interpretation

type BriefActor = MailboxProcessor<Value list>

let actor state : BriefActor =
    BriefActor.Start((fun channel ->
        let rec loop state = async {
            let! input = channel.Receive()
            try return! input |> interpret state |> loop
            with ex ->
                printfn "Actor Error: %s" ex.Message
                return! loop state }
        loop state))

let mutable (registry: Map<string, BriefActor>) = Map.empty

let register name actor = registry <- Map.add name actor registry
