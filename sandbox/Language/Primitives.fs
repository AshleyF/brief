module Primitives

open System
open System.IO
open Structure
open Syntax
open Print
open Interpretation
open Actor

let primitiveState =
    let mutable (primitives : Map<string, (State -> State)>) = Map.empty
    let primitive name fn = primitives <- Map.add name fn primitives

    primitive "map!" (fun s ->
        match s.Stack with
        | String n :: v :: t -> { s with Stack = t; Map = Map.add n v s.Map }
        | _ :: _ :: _ -> failwith "Expected vs"
        | _ -> failwith "Stack underflow")

    primitive "map@" (fun s ->
        match s.Stack with
        | String n :: t ->
            match Map.tryFind n s.Map with
            | Some v -> { s with Stack = v :: t }
            | None -> failwith "Not found" // TODO return flag?
        | _ :: _ -> failwith "Expected s"
        | _ -> failwith "Stack underflow")

    primitive "depth" (fun s ->
        { s with Stack = Number (double s.Stack.Length) :: s.Stack })

    primitive "clear" (fun s -> { s with Stack = [] })

    primitive "dup" (fun s ->
        match s.Stack with
        | x :: t -> { s with Stack = x :: x :: t }
        | _ -> failwith "Stack underflow")

    primitive "drop" (fun s ->
        match s.Stack with
        | _ :: t -> { s with Stack = t }
        | _ -> failwith "Stack underflow")

    primitive "swap" (fun s ->
        match s.Stack with
        | x :: y :: t -> { s with Stack = y :: x :: t }
        | _ -> failwith "Stack underflow")

    primitive "pick" (fun s ->
        match s.Stack with
        | x :: y :: z :: t -> { s with Stack = z :: x :: y :: z :: t }
        | _ -> failwith "Stack underflow")

    primitive "dip" (fun s ->
        match s.Stack with
        | List q :: v :: t -> { s with Stack = t; Continuation = (List.rev q) @ v :: s.Continuation }
        | _ :: _ :: _ -> failwith "Expected vq"
        | _ -> failwith "Stack underflow")

    primitive "if" (fun s ->
        match s.Stack with
        | List q :: List r :: Boolean b :: t ->
            { s with Stack = t; Continuation = (List.rev (if b then q else r)) @ s.Continuation }
        | _ :: _ :: _ -> failwith "Expected vq"
        | _ -> failwith "Stack underflow")

    let binaryOp name op = primitive name (fun s ->
        match s.Stack with
        | Number x :: Number y :: t -> { s with Stack = Number (op y x) :: t }
        | _ :: _ :: _ -> failwith "Expected nn"
        | _ -> failwith "Stack underflow")

    binaryOp "+" (+)
    binaryOp "-" (-)
    binaryOp "*" (*)
    binaryOp "/" (/)
    binaryOp "mod" (%)

    let unaryOp name op = primitive name (fun s ->
        match s.Stack with
        | Number x :: t -> { s with Stack = Number (op x) :: t }
        | _ :: t -> failwith "Expected n"
        | _ -> failwith "Stack underflow")

    unaryOp "sqrt" (Math.Sqrt)
    unaryOp "cbrt" (Math.Cbrt)

    unaryOp "sin" (Math.Sin)
    unaryOp "cos" (Math.Cos)
    unaryOp "tan" (Math.Tan)
    unaryOp "sinh" (Math.Sinh)
    unaryOp "cosh" (Math.Cosh)
    unaryOp "tanh" (Math.Tanh)
    unaryOp "asin" (Math.Asin)
    unaryOp "acos" (Math.Acos)
    unaryOp "atan" (Math.Atan)
    unaryOp "asinh" (Math.Asinh)
    unaryOp "acosh" (Math.Acosh)
    unaryOp "atanh" (Math.Atanh)
    binaryOp "atan2" (fun x y -> Math.Atan2(float x, float y))

    unaryOp "ceil" (Math.Ceiling)
    unaryOp "floor" (Math.Floor)
    unaryOp "trunc" (Math.Truncate)
    unaryOp "round" (Math.Round)

    binaryOp "pow" (fun x y -> Math.Pow(float x, float y))
    unaryOp "ln" (Math.Log)
    unaryOp "log" (Math.Log10)
    unaryOp "log2" (Math.Log2)

    let booleanOp name op = primitive name (fun s ->
        match s.Stack with
        | Boolean x :: Boolean y :: t -> { s with Stack = Boolean (op x y) :: t }
        | _ :: _ :: _ -> failwith "Expected bb"
        | _ -> failwith "Stack underflow")

    booleanOp "and" (&&)
    booleanOp "or" (||)

    primitive "not" (fun s ->
        match s.Stack with
        | Boolean x :: t -> { s with Stack = Boolean (not x) :: t }
        | _ :: _ -> failwith "Expected b"
        | _ -> failwith "Stack underflow")

    let comparisonOp name op = primitive name (fun s ->
        match s.Stack with
        | x :: y :: t -> { s with Stack = Boolean (op y x) :: t }
        | _ -> failwith "Stack underflow")

    comparisonOp "=" (=)
    comparisonOp ">" (>)

    primitive "let" (fun s ->
        match s.Stack with
        | String n :: (List _ as q) :: t -> { s with Dictionary = addWord n q s.Dictionary; Stack = t }
        | String n :: v :: t -> { s with Dictionary = addWord n v s.Dictionary; Stack = t }
        | _ :: _  :: _ -> failwith "Expected sq"
        | _ -> failwith "Stack underflow")

    primitive "eval" (fun s ->
        match s.Stack with
        | String b :: t -> brief b |> interpret { s with Stack = t }
        | _ :: _ -> failwith "Expected s"
        | _ -> failwith "Stack underflow")

    primitive "state" (fun s -> printState s; s)

    primitive "post" (fun s ->
        match s.Stack with
        | String n :: List q :: t ->
            match Map.tryFind n registry with
            | Some actor -> actor.Post (List.rev q); { s with Stack = t }
            | None -> failwith "Actor not found"
        | _ :: _  :: _ -> failwith "Expected ss"
        | _ -> failwith "Stack underflow")

    primitive "load" (fun s ->
        match s.Stack with
        | String p :: t -> File.ReadAllText(sprintf "%s.b" p) |> rep { s with Stack = t }
        | _  :: _ -> failwith "Expected s"
        | _ -> failwith "Stack underflow")

    primitive "type" (fun s ->
        let kind, t =
            match s.Stack with
            | Symbol  _ :: t -> "sym", t
            | Number  _ :: t -> "num", t
            | String  _ :: t -> "str", t
            | Boolean _ :: t -> "bool", t
            | List    _ :: t -> "list", t
            | Map     _ :: t -> "map", t
            | [] -> failwith "Stack underflow"
        { s with Stack = String kind :: t })

    primitive ">sym" (fun s ->
        match s.Stack with
        | Symbol _ :: _ -> s
        | String str :: t ->
            if str |> Seq.exists (System.Char.IsWhiteSpace)
            then failwith "Symbols cannot contain whitespace"
            else { s with Stack = Symbol str :: t }
        | List _ :: t -> failwith "Lists cannot be cast to a Symbol value"
        | Map _ :: t -> failwith "Maps cannot be cast to a Symbol value"
        | v :: t -> { s with Stack = Symbol (stringOfValue v) :: t }
        | _ -> failwith "Stack underflow")

    primitive ">num" (fun s ->
        match s.Stack with
        | Number _ :: _ -> s
        | Symbol y :: t | String y :: t ->
            match System.Double.TryParse y with
            | (true, v) -> { s with Stack = Number v :: t }
            | _ -> failwith "Cannot cast to Number"
        | Boolean b :: t -> { s with Stack = Number (if b then -1. else 0.) :: t}
        | List l :: t -> { s with Stack = Number (List.length l |> float) :: t }
        | Map m :: t -> { s with Stack = Number (Map.count m |> float) :: t }
        | _ -> failwith "Stack underflow")

    primitive ">str" (fun s ->
        match s.Stack with
        | String _ :: _ -> s
        | v :: t -> { s with Stack = String (stringOfValue v) :: t }
        | _ -> failwith "Stack underflow")

    primitive ">bool" (fun s ->
        match s.Stack with
        | Boolean _ :: _ -> s
        | Symbol y :: t | String y :: t ->
            match System.Boolean.TryParse y with
            | (true, v) -> { s with Stack = Boolean v :: t }
            | _ -> failwith "Cannot cast to Number"
        | Number n :: t -> { s with Stack = Boolean (n <> 0.) :: t }
        | List l :: t -> { s with Stack = Boolean (List.isEmpty l |> not) :: t }
        | Map m :: t -> { s with Stack = Boolean (Map.isEmpty m |> not) :: t }
        | _ -> failwith "Stack underflow")

    primitive "split" (fun s ->
        match s.Stack with
        | Symbol y :: t | String y :: t -> { s with Stack = (y |> Seq.toList |> List.map (string >> String) |> List) :: t }
        | _ :: _ -> failwith "Expected s"
        | _ -> failwith "Stack underflow")

    primitive "join" (fun s ->
        let str = function String y | Symbol y -> y | _ -> failwith "Expected List of Strings/Symbols"
        match s.Stack with
        | List l :: t -> { s with Stack = (l |> Seq.map str |> String.concat "" |> String) :: t }
        | _ :: _ -> failwith "Expected l"
        | _ -> failwith "Stack underflow")

    primitive "count" (fun s ->
        match s.Stack with
        | (List l :: _) as t -> { s with Stack = (List.length l |> double |> Number) :: t }
        | (Map m :: _) as t -> { s with Stack = (Map.count m |> double |> Number) :: t }
        | _ :: _ -> failwith "Cannot cast to List"
        | _ -> failwith "Stack underflow")

    primitive "cons" (fun s ->
        match s.Stack with
        | v :: List l :: t -> { s with Stack = List (v :: l) :: t }
        | _ :: _ :: _ -> failwith "Expected vl"
        | _ -> failwith "Stack underflow")

    primitive "snoc" (fun s ->
        match s.Stack with
        | List (h :: t') :: t -> { s with Stack = h :: List t' :: t }
        | List _ :: _ -> failwith "Expected non-empty list"
        | _ :: _ :: _ -> failwith "Expected vl"
        | _ -> failwith "Stack underflow")

    primitive "compose" (fun s ->
        match s.Stack with
        | List q :: List r :: t -> { s with Stack = List (q @ r) :: t }
        | _ :: _ :: _ -> failwith "Expected ll"
        | _ -> failwith "Stack underflow")

    primitive "key?" (fun s ->
        match s.Stack with
        | String k :: (Map m :: _ as t) -> { s with Stack = Boolean (Map.containsKey k m) :: t }
        | _ :: _ :: _ -> failwith "Expected sm"
        | _ -> failwith "Stack underflow")

    primitive "@" (fun s ->
        match s.Stack with
        | String k :: (Map m :: _ as t) ->
            match Map.tryFind k m with
            | Some v -> { s with Stack = v :: t }
            | None -> failwith "Key not found"
        | _ :: _ :: _ -> failwith "Expected sm"
        | _ -> failwith "Stack underflow")

    primitive "!" (fun s ->
        match s.Stack with
        | String k :: v :: Map m :: t -> { s with Stack = Map (Map.add k v m) :: t }
        | _ :: _ :: _ :: _ -> failwith "Expected vsm"
        | _ -> failwith "Stack underflow")

    primitive "words" (fun s ->
        printf "Primitives:"
        s.Primitives |> Map.iter (fun k _ -> printf " %s" k)
        printfn "\nSecondaries:"
        s.Dictionary |> List.iter (Map.iter (fun k v -> printfn "%s %s" k (stringOfValue v)))
        s)

    primitive "word" (fun s ->
        match s.Stack with
        | String n :: t ->
            match Map.tryFind n s.Primitives with
            | Some _ -> printfn "%s Primitive" n
            | None ->
                match tryFindWord n s.Dictionary with
                | Some v -> printfn "%s %s" n (stringOfValue v)
                | None -> printfn "%s Unknown" n
            { s with Stack = t }
        | _ :: _ -> failwith "Expected s"
        | _ -> failwith "Stack underflow")

    primitive "_dropFrame" (fun s -> { s with Dictionary = dropFrame s.Dictionary })

    { emptyState with Primitives = primitives }
