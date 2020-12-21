open System
open Structure
open Prelude
open Interpretation
open Actor
open Primitives

let rep state source = [String source; Symbol "eval"] |> interpret state false

let preludeState = prelude |> Seq.fold rep primitiveState

register "tesla" Tesla.teslaActor

let rec repl state =
    try
        match Console.ReadLine() with
        | "exit" -> ()
        | line -> rep state line |> repl
    with ex -> printfn "Error: %s" ex.Message; repl state

repl preludeState

