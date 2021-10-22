module Print

open System
open Structure

let escape (s: string) =
    s.Replace("\\", "\\\\").Replace("\b", "\\b").Replace("\f", "\\f")
     .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t")
let rec stringOfString s =
    let s' = escape s
    sprintf (if String.exists Char.IsWhiteSpace s' then "\"%s\"" else "'%s") s'
let rec stringOfValue =
    let toHex = Seq.fold (fun state x-> state + sprintf "%02X" x) String.Empty
    function
    | Symbol s      -> sprintf "%s" s
    | Number n      -> sprintf "%g" n
    | String s      -> stringOfString s
    | List   l      -> sprintf "[ %s ]" (stringOfList l)
    | Raw (r, i, c) -> sprintf "RAW[%s]" (toHex r.[i..i + c - 1])
    | Map    m      -> sprintf "{ %s }" (stringOfMap m)
    | Word   w      -> sprintf "(%s)" w.Name
and stringOfList =
    let simplify = List.filter (function Symbol s -> not (s.StartsWith('_')) | _ -> true)
    simplify >> List.map stringOfValue >> String.concat " "
and stringOfRaw = Array.map (fun (b: byte) -> b.ToString("X2")) >> String.concat " "
and stringOfMap = Map.toSeq >> Seq.map (fun (k, v) -> sprintf "%s %s" (stringOfString k) (stringOfValue v)) >> String.concat "  "

let printState s =
    printfn ""
    printfn "--- STATE ------------------------------------------------------"
    printfn "Continuation: [ %s ]" (stringOfList (getContinuation s))
    printfn "Stack: [ %s ]" (stringOfList (getStack s))
    printfn "Map: { %s }" (s |> Map.remove _continuation |> Map.remove _stack |> Map.remove _dictionary |> stringOfMap)
