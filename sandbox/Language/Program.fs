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
    | Primitive of NamedPrimitive // string * (State -> State)
    | Secondary of string * Word list

and Value =
    | Number of double // or primitive closures?
    | String of string // or primitive closures?
    | Boolean of bool
    | List of Value list
    | Map of Map<string, Value>
    | Set of Set<Value> // can't because primitives are not comparable
    | Quotation of Word list

and State = {
    Program: Word list
    Stack: Value list
    Dictionary: Map<string, Value> }

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
    | Literal v :: t -> { state with Stack = v :: state.Stack; Program = t } |> eval
    | Primitive p :: t -> { state with Program = t } |> p.Func |> eval
    | Secondary (_, s) :: t -> { state with Program = s @ t } |> eval
    | [] -> state

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

let init = {
    Program = [Literal (Number 7.2); area]
    Stack = []
    Dictionary = Map.empty }

eval init |> ignore
