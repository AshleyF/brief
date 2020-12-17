open System
open Structure
open Prelude
open Interpretation
open Primitives
open Tesla

let rep state source = [Literal (String source); eval] |> interpret state false

let preludeState = prelude |> Seq.fold rep primitiveState

let rec repl state =
    try
        match Console.ReadLine() with
        | "exit" -> ()
        | line -> rep state line |> repl
    with ex -> printfn "Error: %s" ex.Message; repl state

repl teslaState
