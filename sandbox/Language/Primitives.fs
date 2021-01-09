module Primitives

open System
open System.IO
open Structure
open Syntax
open Print
open Interpretation
open Actor

let primitiveState =

    [
        primitive "!map" (fun s ->
            match getStack s with
            | String n :: v :: t -> setStack s t |> Map.add n v
            | _ :: _ :: _ -> failwith "Expected vs"
            | _ -> failwith "Stack underflow")

        primitive "@map" (fun s ->
            match getStack s with
            | String n :: t ->
                match Map.tryFind n (setStack s t) with
                | Some v -> v :: t |> setStack s
                | None -> failwith "Not found" // TODO return flag?
            | _ :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")

        primitive "dup" (fun s ->
            match getStack s with
            | x :: t -> x :: x :: t |> setStack s
            | _ -> failwith "Stack underflow")

        // let 'drop [k []]
        primitive "drop" (fun s ->
            match getStack s with
            | _ :: t -> setStack s t
            | _ -> failwith "Stack underflow")

        // let 'swap [dip quote]
        primitive "swap" (fun s ->
            match getStack s with
            | x :: y :: t -> y :: x :: t |> setStack s
            | _ -> failwith "Stack underflow")

        primitive "pick" (fun s ->
            match getStack s with
            | x :: y :: z :: t -> z :: x :: y :: z :: t |> setStack s
            | _ -> failwith "Stack underflow")

        // let 'dip [k cake]
        primitive "dip" (fun s ->
            match getStack s with
            | List q :: v :: t -> (List.rev q) @ v :: getContinuation s |> setContinuation (setStack s t)
            | _ :: _ :: _ -> failwith "Expected vq"
            | _ -> failwith "Stack underflow")

        primitive "if" (fun s ->
            match getStack s with
            | List q :: List r :: Number b :: t ->
                (List.rev (if b <> 0.0 then q else r)) @ getContinuation s |> setContinuation (setStack s t)
            | _ :: _ :: _ -> failwith "Expected vq"
            | _ -> failwith "Stack underflow")

        let binaryOp name op = primitive name (fun s ->
            match getStack s with
            | Number x :: Number y :: t -> Number (op y x) :: t |> setStack s
            | _ :: _ :: _ -> failwith "Expected nn"
            | _ -> failwith "Stack underflow")

        binaryOp "+" (+)
        binaryOp "-" (-)
        binaryOp "*" (*)
        binaryOp "/" (/)
        binaryOp "mod" (%)

        let unaryOp name op = primitive name (fun s ->
            match getStack s with
            | Number x :: t -> Number (op x) :: t |> setStack s
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
            match getStack s with
            | Number x :: Number y :: t -> Number (op (int x) (int y) |> double) :: t |> setStack s
            | _ :: _ :: _ -> failwith "Expected bb"
            | _ -> failwith "Stack underflow")

        booleanOp "and" (&&&)
        booleanOp "or" (|||)

        primitive "not" (fun s ->
            match getStack s with
            | Number x :: t -> (Number (~~~(int x) |> double)) :: t |> setStack s
            | _ :: _ -> failwith "Expected b"
            | _ -> failwith "Stack underflow")

        let comparisonOp name op = primitive name (fun s ->
            match getStack s with
            | x :: y :: t -> Number (if (op y x) then -1. else 0.) :: t |> setStack s
            | _ -> failwith "Stack underflow")

        comparisonOp "=" (=)
        comparisonOp ">" (>)

        primitive "let" (fun s ->
            match getStack s with
            | String n :: (List _ as q) :: t -> getDictionary s |> addWord n q |> setDictionary (setStack s t)
            | String n :: v :: t -> getDictionary s |> addWord n v |> setDictionary (setStack s t)
            | _ :: _  :: _ -> failwith "Expected sq"
            | _ -> failwith "Stack underflow")

        primitive "eval" (fun s ->
            match getStack s with
            | String b :: t -> brief b |> interpret (setStack s t)
            | _ :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")

        primitive "print" (fun s ->
            let rec print = function
                | String str -> printf "%s" str
                | List l -> List.iter print l
                | v -> stringOfValue v |> printf "%s"
            match getStack s with
            | v :: t -> print v; setStack s t
            | _ -> failwith "Stack underflow")

        primitive "state" (fun s -> printState s; s)

        primitive "post" (fun s ->
            match getStack s with
            | String n :: List q :: t ->
                match Map.tryFind n registry with
                | Some actor -> actor.Post (List.rev q); setStack s t
                | None -> failwith "Actor not found"
            | _ :: _  :: _ -> failwith "Expected ss"
            | _ -> failwith "Stack underflow")

        primitive "load" (fun s ->
            match getStack s with
            | String p :: t -> File.ReadAllText(sprintf "%s.b" p) |> rep (setStack s t)
            | _  :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")

        primitive "type" (fun s ->
            let kind, t =
                match getStack s with
                | Symbol  _ :: t -> "sym", t
                | Number  _ :: t -> "num", t
                | String  _ :: t -> "str", t
                | List    _ :: t -> "list", t
                | Map     _ :: t -> "map", t
                | Word    _ :: t -> "word", t
                | [] -> failwith "Stack underflow"
            String kind :: t |> setStack s)

        primitive ">sym" (fun s ->
            match getStack s with
            | Symbol _ :: _ -> s
            | String str :: t ->
                if str |> Seq.exists (System.Char.IsWhiteSpace)
                then failwith "Symbols cannot contain whitespace"
                else Symbol str :: t |> setStack s
            | Word w :: t -> Symbol (w.Name) :: t |> setStack s
            | List _ :: t -> failwith "Lists cannot be cast to a Symbol value"
            | Map _ :: t -> failwith "Maps cannot be cast to a Symbol value"
            | v :: t -> Symbol (stringOfValue v) :: t |> setStack s
            | [] -> failwith "Stack underflow")

        primitive ">num" (fun s ->
            match getStack s with
            | Number _ :: _ -> s
            | Symbol y :: t | String y :: t ->
                match System.Double.TryParse y with
                | (true, v) -> Number v :: t |> setStack s
                | _ -> failwith "Cannot cast to Number"
            | List l :: t -> Number (List.length l |> float) :: t |> setStack s
            | Map m :: t -> Number (Map.count m |> float) :: t |> setStack s
            | Word _ :: _ -> failwith "Word cannot be cast to Number value"
            | [] -> failwith "Stack underflow")

        primitive ">str" (fun s ->
            match getStack s with
            | String _ :: _ -> s
            | Word w :: t -> String (w.Name) :: t |> setStack s
            | v :: t -> String (stringOfValue v) :: t |> setStack s
            | [] -> failwith "Stack underflow")

        primitive "split" (fun s ->
            match getStack s with
            | Symbol y :: t | String y :: t -> (y |> Seq.toList |> List.map (string >> String) |> List) :: t |> setStack s
            | _ :: _ -> failwith "Expected s"
            | [] -> failwith "Stack underflow")

        primitive "join" (fun s ->
            let str = function String y | Symbol y -> y | _ -> failwith "Expected List of Strings/Symbols"
            match getStack s with
            | List l :: t -> (l |> Seq.map str |> String.concat "" |> String) :: t |> setStack s
            | _ :: _ -> failwith "Expected l"
            | [] -> failwith "Stack underflow")

        primitive "count" (fun s ->
            match getStack s with
            | (List l :: _) as t -> (List.length l |> double |> Number) :: t |> setStack s
            | (Map m :: _) as t -> (Map.count m |> double |> Number) :: t |> setStack s
            | _ :: _ -> failwith "Cannot cast to List"
            | [] -> failwith "Stack underflow")

        primitive "cons" (fun s ->
            match getStack s with
            | v :: List l :: t -> List (v :: l) :: t |> setStack s
            | _ :: _ :: _ -> failwith "Expected vl"
            | _ -> failwith "Stack underflow")

        primitive "snoc" (fun s ->
            match getStack s with
            | List (h :: t') :: t -> h :: List t' :: t |> setStack s
            | List _ :: _ -> failwith "Expected non-empty list"
            | _ :: _ :: _ -> failwith "Expected vl"
            | _ -> failwith "Stack underflow")

        primitive "compose" (fun s ->
            match getStack s with
            | List q :: List r :: t -> List (q @ r) :: t |> setStack s
            | _ :: _ :: _ -> failwith "Expected ll"
            | _ -> failwith "Stack underflow")

        primitive "key?" (fun s ->
            match getStack s with
            | String k :: (Map m :: _ as t) -> Number (if Map.containsKey k m then -1. else 0.) :: t |> setStack s
            | _ :: _ :: _ -> failwith "Expected sm"
            | _ -> failwith "Stack underflow")

        primitive "@" (fun s ->
            match getStack s with
            | String k :: (Map m :: _ as t) ->
                match Map.tryFind k m with
                | Some v -> v :: t |> setStack s
                | None -> failwith "Key not found"
            | _ :: _ :: _ -> failwith "Expected sm"
            | _ -> failwith "Stack underflow")

        primitive "!" (fun s ->
            match getStack s with
            | String k :: v :: Map m :: t -> Map (Map.add k v m) :: t |> setStack s
            | _ :: _ :: _ :: _ -> failwith "Expected vsm"
            | _ -> failwith "Stack underflow")

        primitive "words" (fun s ->
            getDictionary s |> List.iter (function Map m -> Map.iter (fun k v -> printfn "%s %s" k (stringOfValue v)) m | _ -> failwith "Malformed dictionary")
            s)

        primitive "word" (fun s ->
            match getStack s with
            | String n :: t ->
                match getDictionary s |> tryFindWord n with
                | Some v -> printfn "%s %s" n (stringOfValue v)
                | None -> printfn "%s Unknown" n
                setStack s t
            | _ :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")

        // primitive "k" (fun s ->
        //     match getStack s with
        //     | List q :: _ :: t -> (List.rev q) @ getContinuation s |> setContinuation (setStack s t)
        //     | _ :: _ :: _ -> failwith "Expected lv"
        //     | _ -> failwith "Stack underflow")

        // primitive "cake" (fun s ->
        //     match getStack s with
        //     | List q :: v :: t -> List (v :: q) :: List (q @ [v]) :: t |> setStack s
        //     | _ :: _ :: _ -> failwith "Expected lv"
        //     | _ -> failwith "Stack underflow")

        primitive "_return" (fun s -> setDictionary s (getDictionary s |> dropFrame))


    ] |> addPrimitives emptyState
