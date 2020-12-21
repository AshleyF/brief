module Print

open Structure

let rec stringOfValue = function
    | Number n -> sprintf "%f" n
    | String s -> sprintf "\"%s\"" s
    | Boolean b -> sprintf "%b" b
    | List l -> sprintf "[%s]" (stringOfValues l) // TODO: same syntax as quotation?
    | Map m -> sprintf "MAP: %A" m // TODO
    | Quotation q -> sprintf "[%s]" (stringOfWords q)
and stringOfValues = List.map stringOfValue >> String.concat " " // TODO: unify words/values

and stringOfWord = function
    | Literal lit -> stringOfValue lit
    | Primitive p -> p.Name
    | Secondary (n, q) -> stringOfWords q
and stringOfWords = List.map stringOfWord >> String.concat " "

let printState s =
    let printMap m t = m |> Map.toSeq |> Seq.iter (fun (k, v) -> printfn "  %s -> %s" k (t v))
    printfn ""
    printfn "--- STATE ------------------------------------------------------"
    printfn "Continuation: %s" (stringOfWords s.Continuation)
    printfn "Stack: %s" (stringOfValues s.Stack)
    printfn "Map:"; printMap s.Map stringOfValue
    printfn "Dictionary:"; printMap s.Dictionary stringOfWord

let printDebug s =
    let program = s.Continuation |> List.rev |> stringOfWords
    let stack = stringOfValues s.Stack
    printfn "%s | %s" program stack
