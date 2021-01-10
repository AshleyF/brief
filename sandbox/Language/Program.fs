open System
open Syntax
open Print
open Interpretation
open Actor
open Primitives

printfn "Welcome to Brief"

register "tesla"   Tesla.teslaActor
register "trigger" Trigger.triggerActor
register "remote"  Remote.remoteActor

// while true do
//     let foo = Console.ReadKey()
//     printfn "Key: %A %A %A %b" foo.KeyChar foo.Key foo.Modifiers (Char.IsControl(foo.KeyChar))

type Command = Exit | StepIn | StepOut | StepOver | Interpret

let read () =
    let rec read' line =
        let key = Console.ReadKey()
        if Char.IsControl key.KeyChar then
            match key.Key with
            | ConsoleKey.Escape -> read' String.Empty
            | ConsoleKey.Backspace ->
                let backspace (line: string) =
                    let len = line.Length
                    if len > 0 then line.Substring(0, len - 1) else line
                printf " \b"
                backspace line |> read'
            | ConsoleKey.Enter -> (if line = "exit" then Exit else Interpret), line
            | ConsoleKey.RightArrow -> StepOver, line
            | ConsoleKey.DownArrow -> StepIn, line
            | ConsoleKey.UpArrow -> StepOut, line
            | _ -> printfn "Unknown key: %A" key.Key; read' line
        else read' (sprintf "%s%c" line key.KeyChar)
    read' String.Empty

let rec repl state =
    printDebug None state
    let debug fn = brief >> fn state >> repl 
    try
        match read () with
        | Exit, _ -> ()
        | StepIn, line -> debug stepIn line
        | StepOut, line -> debug stepOut line
        | StepOver, line -> debug stepOver line
        | Interpret, line -> debug interpret line
    with ex -> printfn "Error: %s" ex.Message; repl state

let commandLine =
    let exe = Environment.GetCommandLineArgs().[0]
    Environment.CommandLine.Substring(exe.Length)

printf "Loading Prelude..."
let state = commandLine :: ["load 'Prelude"] |> Seq.fold (fun s -> brief >> interpret s) primitiveState
printfn " Ready"
repl state
