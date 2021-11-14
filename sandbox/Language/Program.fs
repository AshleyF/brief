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
    let continuation = getContinuation s |> List.rev |> stringOfList
    let stack = stringOfList (getStack s)
    Console.ForegroundColor <- ConsoleColor.DarkGray
    Console.Write(continuation)
    Console.ForegroundColor <- if debugging then ConsoleColor.DarkRed else ConsoleColor.DarkBlue
    Console.Write(" | ")
    Console.ForegroundColor <- ConsoleColor.White
    Console.WriteLine(stack)

let rec debugger history state =
    if getContinuation state |> List.length = 0
    then repl history state else
        try
            printPrompt true state
            let key = Console.ReadKey()
            match key.Key with
            | ConsoleKey.Enter -> interpret [] state |> repl history
            | ConsoleKey.RightArrow -> stepOver state |> debugger history
            | ConsoleKey.DownArrow -> stepIn state |> debugger history
            | ConsoleKey.UpArrow -> stepOut state |> debugger history
            | _ -> debugger history state
        with ex -> printfn "Error: %s" ex.Message; state |> setContinuation [] |> repl history

and repl history state =
    if getContinuation state |> List.length > 0
    then debugger history state else
        printPrompt false state
        try
            match Console.ReadLine() with
            | "exit" -> ()
            | "undo" ->
                match history with
                | h :: t -> repl t h
                | _ -> failwith "Empty history"
            | line -> state |> (line |> brief |> interpret) |> repl (state :: history)
        with ex -> printfn "Error: %s" ex.Message; repl history state

let commandLine =
    let exe = Environment.GetCommandLineArgs().[0]
    Environment.CommandLine.Substring(exe.Length)

printf "Loading Prelude..."
let boot =
    "if parse lex read 'prelude.b [ ] -1"
    // "open '../../../boot"
let state = commandLine :: [boot] |> Seq.fold (fun s c -> interpret (brief c) s) primitiveState
printfn " Ready"
repl [] state
