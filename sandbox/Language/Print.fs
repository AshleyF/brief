module Print

open System
open Structure

let rec stringOfString s = sprintf (if String.exists Char.IsWhiteSpace s then "\"%s\"" else "'%s") s
let rec stringOfValue = function
    | Symbol s  -> sprintf "%s" s
    | Number n  -> sprintf "%g" n
    | String s  -> stringOfString s
    | Boolean b -> sprintf "%b" b
    | List l    -> sprintf "[%s]" (stringOfValues l)
    | Map m     -> sprintf "{ %s }" (stringOfMap m)
and stringOfValues = List.map stringOfValue >> String.concat " "
and stringOfMap = Map.toSeq >> Seq.map (fun (k, v) -> sprintf "%s %s" (stringOfString k) (stringOfValue v)) >> String.concat "  "

let printState s =
    printfn ""
    printfn "--- STATE ------------------------------------------------------"
    printfn "Continuation: [%s]" (stringOfValues s.Continuation)
    printfn "Stack: [%s]" (stringOfValues s.Stack)
    printfn "Map: { %s }" (stringOfMap s.Map)
    printf  "Doctionary: "
    List.iter (fun frame -> printf "{ %s }" (stringOfMap frame)) s.Dictionary

let printDebug w s =
    let continuation = s.Continuation |> List.rev |> stringOfValues
    let stack = stringOfValues s.Stack
    match w with
    | Some w -> printfn "%s %s | %s" continuation (stringOfValue w) stack
    | None -> printfn "%s | %s" continuation stack
