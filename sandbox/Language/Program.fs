open System
open System.IO
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
            | line -> state |> (interpret [String line; Symbol "lex"; Symbol "parse"; Symbol "apply"]) |> repl (state :: history)
        with ex -> printfn "Error: %s" ex.Message; repl history state

let commandLine =
    let exe = Environment.GetCommandLineArgs().[0]
    Environment.CommandLine.Substring(exe.Length)

#if DEBUG
let state =
    commandLine :: ["if parse lex read 'prelude.b [ ] -1"] // boot from source: `source 'prelude`.b (applied with `if`)
    |> Seq.fold (fun s c -> interpret (c |> lex |> parse) s) primitiveState
#else
let state =
    use file = File.OpenRead("boot.i")
    use reader = new BinaryReader(file)
    let primMap = primitives |> Seq.map (fun p -> p.Name, Word p) |> Map.ofSeq
    match Serialization.deserialize primMap reader with
    | Map m -> m
    | _ -> failwith "Expected boot image to contain state Map"
#endif
repl [] state
