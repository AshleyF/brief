module Print

open System
open Structure

let rec stringOfString (s: string) =
    let s' = s.Replace("\\", "\\\\").Replace("\b", "\\b").Replace("\f", "\\f").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t")
    sprintf (if String.exists Char.IsWhiteSpace s' then "\"%s\"" else "'%s") s'
let rec stringOfValue = function
    | Symbol s  -> sprintf "%s" s
    | Number n  -> sprintf "%g" n
    | String s  -> stringOfString s
    | List l    -> sprintf "[%s]" (stringOfValues l)
    | Map m     -> sprintf "{ %s }" (stringOfMap m)
    | Word w    -> sprintf "(%s)" w.Name
and stringOfValues = (* List.filter (function Symbol s -> not (s.StartsWith('_')) | _ -> true) >> *) List.map stringOfValue >> String.concat " "
and stringOfMap = Map.toSeq >> Seq.map (fun (k, v) -> sprintf "%s %s" (stringOfString k) (stringOfValue v)) >> String.concat "  "

let printState s =
    printfn ""
    printfn "--- STATE ------------------------------------------------------"
    printfn "Continuation: [%s]" (stringOfValues (getContinuation s))
    printfn "Stack: [%s]" (stringOfValues (getStack s))
    printfn "Map: { %s }" (s |> Map.remove _continuation |> Map.remove _stack |> Map.remove _dictionary |> stringOfMap)

let printDebug w s =
    let continuation = getContinuation s |> List.rev |> stringOfValues
    let stack = stringOfValues (getStack s)
    match w with
    | Some w -> printfn "\n%s %s | %s" continuation (stringOfValue w) stack
    | None -> printfn "\n%s | %s" continuation stack
