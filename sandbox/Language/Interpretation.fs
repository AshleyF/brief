module Interpretation

open Structure
open Print

let rec interpret state debug stream =
    let word state = function
        | Literal v -> { state with Stack = v :: state.Stack }
        | Primitive p -> p.Func state
        | Secondary (_, s) -> { state with Continuation = s @ state.Continuation }
    if debug then
        printState state
        System.Console.ReadKey() |> ignore
    else printDebug state
    match state.Continuation with
    | [] ->
        match Seq.tryHead stream with
        | Some w -> interpret (word state w)debug (Seq.tail stream)
        | None -> state
    | w :: c -> interpret (word { state with Continuation = c } w) debug stream
