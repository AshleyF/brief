open System

type NamedPrimitive(name: string, func: State -> State)= 
    member _.Name = name
    member _.Func = func
    override this.Equals(o) =
        match o with
            | :? NamedPrimitive as p -> this.Name = p.Name
            | _ -> false
    override this.GetHashCode() = hash (this.Name)
    interface IComparable with
        override this.CompareTo(o) =
            match o with
                | :? NamedPrimitive as p -> compare this.Name p.Name
                | _ -> -1

and Word =
    | Literal of Value
    | Primitive of NamedPrimitive
    | Secondary of string * Word list

and Value =
    | Number of double // or primitive closures?
    | String of string // or primitive closures?
    | Boolean of bool
    | List of Value list
    | Map of Map<string, Value>
    | Set of Set<Value>
    | Quotation of Word list

and State = {
    Continuation: Word list
    Stack: Value list
    Dictionary: Map<string, Value> }
let emptyState = { Continuation = []; Stack = []; Dictionary = Map.empty }

let rec stringOfValue = function
    | Number n -> sprintf "%f" n
    | String s -> sprintf "\"%s\"" s
    | Boolean b -> sprintf "%b" b
    | List l -> sprintf "[%s]" (stringOfValues l) // TODO: same syntax as quotation?
    | Map m -> sprintf "MAP: %A" m // TODO
    | Set s -> sprintf "SET: %A" s // TODO
    | Quotation q -> sprintf "[%s]" (stringOfWords q)
and stringOfValues = List.map stringOfValue >> String.concat " " // TODO: unify words/values

and stringOfWord = function
    | Literal lit -> stringOfValue lit
    | Primitive p -> p.Name
    | Secondary (n, _) -> n
and stringOfWords = List.map stringOfWord >> String.concat " "

let printState s =
    printfn ""
    printfn "--- STATE ------------------------------------------------------"
    printfn "Continuation: %s" (stringOfWords s.Continuation)
    printfn "Stack: %s" (stringOfValues s.Stack)
    printfn "Dictionary:"
    s.Dictionary |> Map.toSeq |> Seq.iter (fun (k, v) -> printfn "  %s -> %s" k (stringOfValue v))

let printDebug s =
    let program = s.Continuation |> List.rev |> stringOfWords
    let stack = stringOfValues s.Stack
    printfn "%s | %s" program stack

let rec eval stream state =
    let word state = function
        | Literal v -> { state with Stack = v :: state.Stack }
        | Primitive p -> p.Func state
        | Secondary (_, s) -> { state with Continuation = s @ state.Continuation }
    printDebug state
    Console.ReadLine() |> ignore
    match state.Continuation with
    | [] ->
        match Seq.tryHead stream with
        | Some w -> word state w |> eval (Seq.tail stream)
        | None -> state
    | w :: c -> word { state with Continuation = c } w |> eval stream

let dup = Primitive (NamedPrimitive ("dup", (fun s ->
    match s.Stack with
    | x :: t -> { s with Stack = x :: x :: t }
    | _ -> failwith "Stack underflow")))

let mult = Primitive (NamedPrimitive ("*", (fun s ->
    match s.Stack with
    | Number x :: Number y :: t -> { s with Stack = Number (x * y) :: t }
    | _ -> failwith "Stack underflow")))

let pi = Secondary ("pi", [Literal (Number 3.14159265)])
let sq = Secondary ("sq", [dup; mult])
let area = Secondary ("area", [sq; pi; mult])

eval [Literal (Number 7.2); area] emptyState |> printDebug
