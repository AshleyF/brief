open System
open Print
open Interpretation
open Actor
open Primitives

register "tesla"   Tesla.teslaActor
register "trigger" Trigger.triggerActor
register "remote"  Remote.remoteActor

let rec repl state =
    printDebug None state
    try
        match Console.ReadLine() with
        | "exit" -> ()
        | line -> rep state line |> repl
    with ex -> printfn "Error: %s" ex.Message; repl state

let commandLine =
    let exe = Environment.GetCommandLineArgs().[0]
    Environment.CommandLine.Substring(exe.Length)

let state = commandLine :: ["load 'Prelude"] |> Seq.fold rep primitiveState
printfn "Welcome to Brief"
repl state
