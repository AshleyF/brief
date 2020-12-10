type Value =
    | Number of double // or primitive closures?
    | String of string // or primitive closures?
    | Quotation of Word list

and Word =
    | Primitive of string * (State -> State)
    | Secondary of string * Word list

and State = {
    Program: Word list
    Stack: Value list
    Dictionary: Map<string, Value> }

let stringOfWord = function Primitive (n, _) | Secondary (n, _) -> n
let stringOfWords = List.map stringOfWord >> String.concat " "

let stringOfValue = function
    | Number n -> sprintf "%f" n
    | String s -> sprintf "\"%s\"" s
    | Quotation q -> sprintf "[%s]" (stringOfWords q)
let stringOfValues = List.map stringOfValue >> String.concat " " // TODO: unify words/values

let printState s =
    printfn ""
    printfn "--- STATE ------------------------------------------------------"
    printfn "Program: %s" (stringOfWords s.Program)
    printfn "Stack: %s" (stringOfValues s.Stack)
    printfn "Dictionary:"
    s.Dictionary |> Map.toSeq |> Seq.iter (fun (k, v) -> printfn "  %s -> %s" k (stringOfValue v))

let printDebug s =
    let program = s.Program |> List.rev |> stringOfWords
    let stack = stringOfValues s.Stack
    printfn "%s | %s" program stack

let rec eval state =
    printDebug state
    match state.Program with
    | Primitive (_, p) :: t -> { state with Program = t } |> p |> eval
    | Secondary (_, s) :: t -> { state with Program = s @ t } |> eval
    | [] -> state

let number x = Primitive (stringOfValue (Number x), (fun s -> { s with Stack = Number x :: s.Stack }))
let string x = Primitive (stringOfValue (String x), (fun s -> { s with Stack = String x :: s.Stack })) // TODO: unify

let dup = Primitive ("dup", (fun s ->
    match s.Stack with
    | x :: t -> { s with Stack = x :: x :: t }
    | _ -> failwith "Stack underflow"))

let mult = Primitive ("*", (fun s ->
    match s.Stack with
    | Number x :: Number y :: t -> { s with Stack = Number (x * y) :: t }
    | _ -> failwith "Stack underflow"))

let pi = Secondary ("pi", [number 3.14159265])
let sq = Secondary ("sq", [dup; mult])
let area = Secondary ("area", [sq; pi; mult])

let init = {
    Program = [number 7.2; area]
    Stack = []
    Dictionary = Map.empty }

eval init
