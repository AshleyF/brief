module Interpretation

open Structure
open Print

let rec eval state debug stream =
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
        | Some w -> eval (word state w)debug (Seq.tail stream)
        | None -> state
    | w :: c -> eval (word { state with Continuation = c } w) debug stream
