module Primitives

open System
open System.IO
open System.Diagnostics
open Structure
open Serialization
open Syntax
open Print
open Actor

let rec primitives =
    [
        primitive "!map" (fun s ->
            match getStack s with
            | String n :: v :: t -> setStack t s |> Map.add n v
            | _ :: _ :: _ -> failwith "Expected vs"
            | _ -> failwith "Stack underflow")

        primitive "@map" (fun s ->
            match getStack s with
            | String n :: t ->
                match Map.tryFind n (setStack t s) with
                | Some v -> setStack (v :: t) s
                | None -> failwith "Not found" // TODO return flag?
            | _ :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")

        primitive "dup" (fun s ->
            match getStack s with
            | x :: t -> setStack (x :: x :: t) s
            | _ -> failwith "Stack underflow")

        // let 'drop [k []]
        primitive "drop" (fun s ->
            match getStack s with
            | _ :: t -> setStack t s
            | _ -> failwith "Stack underflow")

        // let 'swap [dip quote]
        primitive "swap" (fun s ->
            match getStack s with
            | x :: y :: t -> setStack (y :: x :: t) s
            | _ -> failwith "Stack underflow")

        primitive "pick" (fun s ->
            match getStack s with
            | x :: y :: z :: t -> setStack (z :: x :: y :: z :: t) s
            | _ -> failwith "Stack underflow")

        // let 'dip [k cake]
        primitive "dip" (fun s ->
            match getStack s with
            | List q :: v :: t -> setStack t s |> updateContinuation (fun c -> (List.rev q) @ v :: c)
            | _ :: _ :: _ -> failwith "Expected vq"
            | _ -> failwith "Stack underflow")

        primitive "if" (fun s ->
            match getStack s with
            | List q :: List r :: Number b :: t -> setStack t s |> updateContinuation (fun c -> List.rev (if b <> 0.0 then q else r) @ c)
            | _ :: _ :: _ -> failwith "Expected vq"
            | _ -> failwith "Stack underflow")

        let binaryOp name op = primitive name (fun s ->
            match getStack s with
            | Number x :: Number y :: t -> setStack (Number (op y x) :: t) s
            | _ :: _ :: _ -> failwith "Expected nn"
            | _ -> failwith "Stack underflow")

        binaryOp "+" (+)
        binaryOp "-" (-)
        binaryOp "*" (*)
        binaryOp "/" (/)
        binaryOp "mod" (%)

        let unaryOp name op = primitive name (fun s ->
            match getStack s with
            | Number x :: t -> setStack (Number (op x) :: t) s
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
            | Number x :: Number y :: t -> setStack (Number (op (int x) (int y) |> double) :: t) s
            | _ :: _ :: _ -> failwith "Expected bb"
            | _ -> failwith "Stack underflow")

        booleanOp "and" (&&&)
        booleanOp "or" (|||)

        primitive "not" (fun s ->
            match getStack s with
            | Number x :: t -> setStack ((Number (~~~(int x) |> double)) :: t) s
            | _ :: _ -> failwith "Expected b"
            | _ -> failwith "Stack underflow")

        let comparisonOp name op = primitive name (fun s ->
            match getStack s with
            | x :: y :: t -> setStack (Number (if (op y x) then -1. else 0.) :: t) s
            | _ -> failwith "Stack underflow")

        comparisonOp "=" (=)
        comparisonOp ">" (>)

        primitive "let" (fun s ->
            match getStack s with
            | String n :: (List _ as q) :: t -> setStack t s |> addWord n q
            | String n :: v :: t -> setStack t s |> addWord n v
            | _ :: _  :: _ -> failwith "Expected sq"
            | _ -> failwith "Stack underflow")

        primitive "print" (fun s ->
            let rec print = function
                | String str -> printf "%s" str
                | List l -> List.iter print l
                | v -> stringOfValue v |> printf "%s"
            match getStack s with
            | v :: t -> print v; setStack t s
            | _ -> failwith "Stack underflow")

        primitive "state" (fun s -> printState s; s)

        primitive "post" (fun s ->
            match getStack s with
            | String n :: List q :: t ->
                match Map.tryFind n registry with
                | Some actor -> actor.Post (List.rev q); setStack t s
                | None -> failwith "Actor not found"
            | _ :: _  :: _ -> failwith "Expected ss"
            | _ -> failwith "Stack underflow")

        primitive "read" (fun s ->
            match getStack s with
            | String p :: t -> setStack ((File.ReadAllText(p) |> String) :: t) s
            | _  :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")

        // reimplemented in brief.b
        primitive "lex" (fun s ->
            printfn "!!! F# Lex !!!"
            match getStack s with
            | String b :: t -> setStack ((lex b |> Seq.map String |> List.ofSeq |> List) :: t) s
            | _  :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")

        // reimplemented in brief.b
        primitive "parse" (fun s ->
            printfn "!!! F# Parse !!!"
            let toString = function String s -> s | _ -> failwith "Expected String"
            match getStack s with
            | List l :: t -> setStack ((l |> Seq.map toString |> parse |> Seq.rev |> List.ofSeq |> List) :: t) s
            | _  :: _ -> failwith "Expected l"
            | _ -> failwith "Stack underflow")

        let stopwatch = new Stopwatch();

        primitive "stopwatch-reset" (fun s -> stopwatch.Reset(); stopwatch.Start(); s)

        primitive "stopwatch-elapsed" (fun s ->
            pushStack (stopwatch.ElapsedMilliseconds |> double |> Number) s)

        primitive "steps-reset" (fun s -> Interpretation.stepCount <- 0L; s)

        primitive "steps-count" (fun s -> pushStack (Interpretation.stepCount |> double |> Number) s)

        primitive "type" (fun s ->
            let kind, t =
                match getStack s with
                | Symbol  _ :: t -> "sym", t
                | Number  _ :: t -> "num", t
                | String  _ :: t -> "str", t
                | List    _ :: t -> "list", t
                | Raw     _ :: t -> "raw", t
                | Map     _ :: t -> "map", t
                | Word    _ :: t -> "word", t
                | [] -> failwith "Stack underflow"
            setStack (String kind :: t) s)

        primitive ">sym" (fun s ->
            match getStack s with
            | Symbol _ :: _ -> s
            | String str :: t ->
                if str |> Seq.exists (System.Char.IsWhiteSpace)
                then failwith "Symbols cannot contain whitespace"
                else setStack (Symbol str :: t) s
            | Word w :: t -> setStack (Symbol (w.Name) :: t) s
            | List _ :: t -> failwith "Lists cannot be cast to a Symbol value"
            | Map _ :: t -> failwith "Maps cannot be cast to a Symbol value"
            | v :: t -> setStack (Symbol (stringOfValue v) :: t) s
            | [] -> failwith "Stack underflow")

        primitive ">num" (fun s ->
            match getStack s with
            | Number _ :: _ -> s
            | Symbol y :: t | String y :: t ->
                match System.Double.TryParse y with
                | (true, v) -> setStack (Number v :: t) s
                | _ -> failwith "Cannot cast to Number"
            | List l :: t -> setStack (Number (List.length l |> float) :: t) s
            | Raw (_, _, c) :: t -> setStack (Number (float c) :: t) s
            | Map m :: t -> setStack (Number (Map.count m |> float) :: t) s
            | Word _ :: _ -> failwith "Word cannot be cast to Number value"
            | [] -> failwith "Stack underflow")

        primitive ">num?" (fun s ->
            match getStack s with
            | Symbol y :: t | String y :: t ->
                match System.Double.TryParse y with
                | (true, v) -> setStack (Number -1. :: Number v :: t) s
                | _ -> setStack (Number 0. :: t) s
            | t -> setStack (Number 0. :: t) s)

        primitive ">str" (fun s ->
            match getStack s with
            | String _ :: _ -> s
            | Word w :: t -> setStack (String (w.Name) :: t) s
            | v :: t -> setStack (String (stringOfValue v) :: t) s
            | [] -> failwith "Stack underflow")

        primitive "split" (fun s ->
            match getStack s with
            | Symbol y :: t | String y :: t -> setStack ((y |> Seq.toList |> List.map (string >> String) |> List) :: t) s
            | _ :: _ -> failwith "Expected s"
            | [] -> failwith "Stack underflow")

        primitive "join" (fun s ->
            let str = function String y | Symbol y -> y | _ -> failwith "Expected List of Strings/Symbols"
            match getStack s with
            | List l :: t -> setStack ((l |> Seq.map str |> String.concat "" |> String) :: t) s
            | _ :: _ -> failwith "Expected l"
            | [] -> failwith "Stack underflow")

        primitive "count" (fun s ->
            match getStack s with
            | (List l :: _) as t -> setStack ((List.length l |> double |> Number) :: t) s
            | (Map m :: _) as t -> setStack ((Map.count m |> double |> Number) :: t) s
            | _ :: _ -> failwith "Cannot cast to List"
            | [] -> failwith "Stack underflow")

        primitive "cons" (fun s ->
            match getStack s with
            | v :: List l :: t -> setStack (List (v :: l) :: t) s
            | _ :: _ :: _ -> failwith "Expected vl"
            | _ -> failwith "Stack underflow")

        primitive "snoc" (fun s ->
            match getStack s with
            | List (h :: t') :: t -> setStack (h :: List t' :: t) s
            | List _ :: _ -> failwith "Expected non-empty list"
            | _ :: _ :: _ -> failwith "Expected vl"
            | _ -> failwith "Stack underflow")

        primitive "prepose" (fun s ->
            match getStack s with
            | List q :: List r :: t -> setStack (List (q @ r) :: t) s
            | _ :: _ :: _ -> failwith "Expected ll"
            | _ -> failwith "Stack underflow")

        primitive "key?" (fun s ->
            match getStack s with
            | String k :: (Map m :: _ as t) -> setStack (Number (if Map.containsKey k m then -1. else 0.) :: t) s
            | _ :: _ :: _ -> failwith "Expected sm"
            | _ -> failwith "Stack underflow")

        primitive "@" (fun s ->
            match getStack s with
            | String k :: (Map m :: _ as t) ->
                match Map.tryFind k m with
                | Some v -> setStack (v :: t) s
                | None -> failwith "Key not found"
            | _ :: _ :: _ -> failwith "Expected sm"
            | _ -> failwith "Stack underflow")

        primitive "!" (fun s ->
            match getStack s with
            | String k :: v :: Map m :: t -> setStack (Map (Map.add k v m) :: t) s
            | _ :: _ :: _ :: _ -> failwith "Expected vsm"
            | _ -> failwith "Stack underflow")

        primitive "words" (fun s ->
            getDictionary s |> Map.iter (fun k v -> printfn "%s %s" k (stringOfValue v)); s)

        primitive "word" (fun s ->
            match getStack s with
            | String n :: t ->
                match tryFindWord n s with
                | Some v -> printfn "%s %s" n (stringOfValue v)
                | None -> printfn "%s Unknown" n
                setStack t s
            | _ :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")

        // primitive "k" (fun s -> // TODO updateContinuation with _return or _soft_break, etc. also
        //     match getStack s with
        //     | List q :: _ :: t -> (List.rev q) @ getContinuation s |> setContinuation (setStack s t)
        //     | _ :: _ :: _ -> failwith "Expected lv"
        //     | _ -> failwith "Stack underflow")

        // primitive "cake" (fun s ->
        //     match getStack s with
        //     | List q :: v :: t -> List (v :: q) :: List (q @ [v]) :: t |> setStack s
        //     | _ :: _ :: _ -> failwith "Expected lv"
        //     | _ -> failwith "Stack underflow")

        primitive "save" (fun s ->
            match getStack s with
            | String n :: t ->
                let s' = setStack t s
                use writer = new BinaryWriter(File.OpenWrite(sprintf "%s" n))
                serialize writer (Map s')
                s'
            | _ :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")

        primitive "open" (fun s ->
            match getStack s with
            | String n :: t ->
                let s' = setStack t s
                let primMap = primitives |> Seq.map (fun p -> p.Name, Word p) |> Map.ofSeq
                use reader = new BinaryReader(File.OpenRead(sprintf "%s" n))
                match deserialize primMap reader with
                | Map m -> m
                | _ -> failwith "Invalid image"
            | _ :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")

        primitive "load" (fun s ->
            match getStack s with
            | String n :: t ->
                use reader = new BinaryReader(File.OpenRead(sprintf "%s" n))
                let bytes = reader.ReadBytes(reader.ReadInt32())
                setStack (Raw (bytes, 0, bytes.Length) :: t) s
            | _ :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")

        primitive "store" (fun s ->
            match getStack s with
            | String n :: Raw (r, i, c) :: t ->
                let s' = setStack t s
                use file = File.OpenWrite(sprintf "%s" n)
                file.Write(r, i, c)
                s'
            | _ :: _ :: _ -> failwith "Expected s r"
            | _ -> failwith "Stack underflow")
    ]
    
let primitiveState = addPrimitives emptyState primitives
