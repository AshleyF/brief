open System
open Structure
open Syntax
open Print
open Interpretation
open Actor
open Primitives

printfn "Welcome to Brief"

register "tesla"   Tesla.teslaActor
register "trigger" Trigger.triggerActor
register "remote"  Remote.remoteActor

let printPrompt debugging s =
    let continuation = getContinuation s |> List.rev |> stringOfValues
    let stack = stringOfValues (getStack s)
    Console.ForegroundColor <- ConsoleColor.DarkGray
    Console.Write(continuation)
    Console.ForegroundColor <- if debugging then ConsoleColor.DarkRed else ConsoleColor.DarkBlue
    Console.Write(" | ")
    Console.ForegroundColor <- ConsoleColor.White
    Console.WriteLine(stack)

let rec debugger state =
    if getContinuation state |> List.length = 0
    then repl state else
        try
            printPrompt true state
            let key = Console.ReadKey()
            match key.Key with
            | ConsoleKey.Enter -> interpret [] state |> repl
            | ConsoleKey.RightArrow -> stepOver state |> debugger
            | ConsoleKey.DownArrow -> stepIn state |> debugger
            | ConsoleKey.UpArrow -> stepOut state |> debugger
            | _ -> debugger state
        with ex -> printfn "Error: %s" ex.Message; state |> setContinuation [] |> repl

and repl state =
    if getContinuation state |> List.length > 0
    then debugger state else
        printPrompt false state
        try
            match Console.ReadLine() with
            | "exit" -> ()
            | line -> state |> (line |> brief |> interpret) |> repl
        with ex -> printfn "Error: %s" ex.Message; repl state

let commandLine =
    let exe = Environment.GetCommandLineArgs().[0]
    Environment.CommandLine.Substring(exe.Length)

printf "Loading Prelude..."
let boot =
    "if parse lex read 'prelude.b [ ] -1"
    // "open 'boot.i"
let state = commandLine :: [boot] |> Seq.fold (fun s c -> interpret (brief c) s) primitiveState
printfn " Ready"
repl state
