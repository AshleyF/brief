module Interpretation

open Structure
open Print

let rec interpret state stream =
    let word state w = 
        // printDebug (Some w) state
        match w with
        | Symbol s ->
            match tryFindWord s (getDictionary state) with
            | Some (List l) ->
                let continuation = List.rev l @ Symbol "_dropFrame" :: getContinuation state |> setContinuation state
                setDictionary continuation (getDictionary state |> addFrame)
            | Some (Word w) -> w.Func state
            | Some v -> v :: getStack state |> setStack state
            | None -> failwith (sprintf "Unknown word '%s'" s)
        | v -> v :: getStack state |> setStack state
    match getContinuation state with
    | [] ->
        match Seq.tryHead stream with
        | Some w -> interpret (word state w) (Seq.tail stream)
        | None ->
            // printDebug None state
            state
    | w :: c -> interpret (word (setContinuation state c) w) stream

let rep state source = [String source; Symbol "eval"] |> interpret state
