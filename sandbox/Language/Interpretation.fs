module Interpretation

open Structure
open Print

let rec interpret state debug stream =
    let word state = function
        | Symbol s ->
            match Map.tryFind s state.Dictionary with
            | Some (List l) -> { state with Continuation = l @ state.Continuation }
            | Some v -> { state with Continuation = v :: state.Continuation }
            | None ->
                match Map.tryFind s state.Primitives with
                | Some p -> p state
                | None -> failwith (sprintf "Unknown word '%s'" s)
        | v -> { state with Stack = v :: state.Stack }
    if debug then
        printState state
        // System.Console.ReadKey() |> ignore
    else printDebug state
    match state.Continuation with
    | [] ->
        match Seq.tryHead stream with
        | Some w -> interpret (word state w) debug (Seq.tail stream)
        | None -> state
    | w :: c -> interpret (word { state with Continuation = c } w) debug stream
