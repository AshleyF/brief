module Print

open Structure

let rec stringOfValue = function
    | Symbol s  -> sprintf "%s" s
    | Number n  -> sprintf "%f" n
    | String s  -> sprintf "\"%s\"" s
    | Boolean b -> sprintf "%b" b
    | List l    -> sprintf "[%s]" (stringOfValues l) // TODO: same syntax as quotation?
    | Map m     -> sprintf "MAP: %A" m // TODO
and stringOfValues = List.map stringOfValue >> String.concat " " // TODO: unify words/values

let printState s =
    let printMap m t = m |> Map.toSeq |> Seq.iter (fun (k, v) -> printfn "  %s -> %s" k (t v))
    printfn ""
    printfn "--- STATE ------------------------------------------------------"
    printfn "Continuation: %s" (stringOfValues s.Continuation)
    printfn "Stack: %s" (stringOfValues s.Stack)
    printfn "Map:"; printMap s.Map stringOfValue
    printfn "Dictionary:"; printMap s.Dictionary stringOfValue

let printDebug s =
    let program = s.Continuation |> List.rev |> stringOfValues
    let stack = stringOfValues s.Stack
    printfn "%s | %s" program stack
