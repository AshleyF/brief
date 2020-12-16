open System
open Structure
open Print
open Prelude
open Interpretation
open Primitives

let rep state source = [Literal (String source); eval] |> interpret state false

let preludeState = prelude |> Seq.fold rep primitiveState

let rec repl state = Console.ReadLine() |> rep state |> repl

repl preludeState
