module Interpretation

open Structure
open Print

let rec interpret state stream =
    let word state w = 
        // printDebug (Some w) state
        match w with
        | Symbol s ->
            match tryFindWord s state.Dictionary with
            | Some (List l) ->
                { state with Dictionary = addFrame state.Dictionary
                             Continuation = List.rev l @ Symbol "_dropFrame" :: state.Continuation }
            | Some v -> { state with Dictionary = addFrame state.Dictionary
                                     Continuation = v :: Symbol "_dropFrame" :: state.Continuation }
            | None ->
                match Map.tryFind s state.Primitives with
                | Some p -> p state
                | None -> failwith (sprintf "Unknown word '%s'" s)
        | v -> { state with Stack = v :: state.Stack }
    match state.Continuation with
    | [] ->
        match Seq.tryHead stream with
        | Some w -> interpret (word state w) (Seq.tail stream)
        | None ->
            // printDebug None state
            state
    | w :: c -> interpret (word { state with Continuation = c } w) stream

let rep state source = [String source; Symbol "eval"] |> interpret state
