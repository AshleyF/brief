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

and Value = // v
    | Number of double // n
    | String of string // s
    | Boolean of bool // b
    | List of Value list // l
    | Map of Map<string, Value> // m
    | Set of Set<Value> // t
    | Quotation of Word list // q

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

let depth = Primitive (NamedPrimitive ("depth", (fun s ->
    { s with Stack = Number (double s.Stack.Length) :: s.Stack })))

let clear = Primitive (NamedPrimitive ("clear", (fun s -> { s with Stack = [] })))

let dup = Primitive (NamedPrimitive ("dup", (fun s ->
    match s.Stack with
    | x :: t -> { s with Stack = x :: x :: t }
    | _ -> failwith "Stack underflow")))

let drop = Primitive (NamedPrimitive ("drop", (fun s ->
    match s.Stack with
    | _ :: t -> { s with Stack = t }
    | _ -> failwith "Stack underflow")))

let i = Primitive (NamedPrimitive ("i", (fun s ->
    match s.Stack with
    | Quotation q :: t -> { s with Stack = t; Continuation = q @ s.Continuation }
    | _ :: t -> failwith "Expected q"
    | _ -> failwith "Stack underflow")))

let dip = Primitive (NamedPrimitive ("dip", (fun s ->
    match s.Stack with
    | Quotation q :: v :: t -> { s with Stack = t; Continuation = q @ Literal v :: s.Continuation }
    | _ :: _ :: t -> failwith "Expected qv"
    | _ -> failwith "Stack underflow")))

let unaryOp name op = Primitive (NamedPrimitive (name, (fun s ->
    match s.Stack with
    | Number x :: t -> { s with Stack = Number (op x) :: t }
    | _ :: t -> failwith "Expected n"
    | _ -> failwith "Stack underflow")))

let binaryOp name op = Primitive (NamedPrimitive (name, (fun s ->
    match s.Stack with
    | Number x :: Number y :: t -> { s with Stack = Number (op x y) :: t }
    | _ :: _ :: t -> failwith "Expected nn"
    | _ -> failwith "Stack underflow")))

let add = binaryOp "+" (+)
let sub = binaryOp "-" (+)
let mul = binaryOp "*" (*)
let div = binaryOp "/" (/)

let chs = unaryOp "chs" (fun n -> -n)
let recip = unaryOp "recip" (fun n -> 1. / n)
let abs = unaryOp "abs" (fun n -> abs n)

let pi = Secondary ("pi", [Literal (Number Math.PI)])
let e = Secondary ("e", [Literal (Number Math.E)])

let sq = Secondary ("sq", [dup; mul])
let area = Secondary ("area", [sq; pi; mul])

eval [Literal (Number 7.2); Literal (String "ashleyf"); Literal (Quotation [area]); dip] emptyState |> printDebug
