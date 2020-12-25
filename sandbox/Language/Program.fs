open System
open Structure
open Prelude
open Interpretation
open Actor
open Primitives

register "tesla" Tesla.teslaActor
register "trigger" Trigger.triggerActor

let rep state source = [String source; Symbol "eval"] |> interpret state

let speech = Remote.remoteActor "127.0.0.1" 11411

let rec repl state =
    try
        match Console.ReadLine() with
        | "exit" -> ()
        | line -> rep state line |> repl
    with ex -> printfn "Error: %s" ex.Message; repl state

let commandLine =
    let exe = Environment.GetCommandLineArgs().[0]
    Environment.CommandLine.Substring(exe.Length)

commandLine :: prelude |> Seq.fold rep primitiveState |> repl
