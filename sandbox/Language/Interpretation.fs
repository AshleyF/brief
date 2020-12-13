module Interpretation

open Structure
open Print

let rec eval debug stream state =
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
        | Some w -> word state w |> eval debug (Seq.tail stream)
        | None -> state
    | w :: c -> word { state with Continuation = c } w |> eval debug stream
