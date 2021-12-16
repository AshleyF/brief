﻿module Primitives

open System
open System.IO
open System.Collections.Generic;
open System.Diagnostics
open System.Text
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
            | _ -> failwith "Stack unerflow")

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
            | List q :: v :: t -> setStack t s |> updateContinuation (fun c -> q @ v :: c)
            | _ :: _ :: _ -> failwith "Expected vq"
            | _ -> failwith "Stack underflow")

        primitive "if" (fun s ->
            match getStack s with
            | List q :: List r :: Number b :: t -> setStack t s |> updateContinuation (fun c -> (if b <> 0.0 then r else q) @ c)
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
            | Number x :: Number y :: t -> setStack (Number (op (int64 x) (int64 y) |> double) :: t) s
            | _ :: _ :: _ -> failwith "Expected bb"
            | _ -> failwith "Stack underflow")

        booleanOp "and" (&&&)
        booleanOp "or" (|||)

        primitive "not" (fun s ->
            match getStack s with
            | Number x :: t -> setStack ((Number (~~~(int64 x) |> double)) :: t) s
            | _ :: _ -> failwith "Expected b"
            | _ -> failwith "Stack underflow")

        let shiftOp name op = primitive name (fun s ->
            match getStack s with
            | Number x :: Number y :: t -> setStack ((Number (op (int64 y) (int32 x) |> double)) :: t) s
            | _ :: _ :: _ -> failwith "Expected nn"
            | _ -> failwith "Stack underflow")

        shiftOp ">>>" (>>>)
        shiftOp "<<<" (<<<)

        let comparisonOp name op = primitive name (fun s ->
            match getStack s with
            | x :: y :: t -> setStack (Number (if (op y x) then -1. else 0.) :: t) s
            | _ -> failwith "Stack underflow")

        comparisonOp "=" (=)
        comparisonOp ">" (>)

        primitive "let" (fun s ->
            match getStack s with
            | (List _ as q) :: String n :: t -> setStack t s |> addWord n q
            | v :: String n :: t -> setStack t s |> addWord n v
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

        primitive "psint-state" (fun s -> printState s; s)

        primitive "post" (fun s ->
            match getStack s with
            | String n :: List q :: t ->
                match Map.tryFind n registry with
                | Some actor -> actor.Post q; setStack t s
                | None -> failwith "Actor not found"
            | _ :: _  :: _ -> failwith "Expected ss"
            | _ -> failwith "Stack underflow")

        primitive "read" (fun s ->
            match getStack s with
            | String p :: t -> setStack ((File.ReadAllText(p) |> String) :: t) s
            | _  :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")

#if DEBUG
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
            | List l :: t -> setStack ((l |> Seq.map toString |> parse |> List.ofSeq |> List) :: t) s
            | _  :: _ -> failwith "Expected l"
            | _ -> failwith "Stack underflow")
#endif

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
                | String  _ :: t -> "str", t
                | Number  _ :: t -> "num", t
                | List    _ :: t -> "list", t
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

        primitive ">str" (fun s ->
            match getStack s with
            | String _ :: _ -> s
            | Word w :: t -> setStack (String (w.Name) :: t) s
            | v :: t -> setStack (String (stringOfValue v) :: t) s
            | [] -> failwith "Stack underflow")

        primitive ">num" (fun s ->
            match getStack s with
            | Number _ :: _ -> s
            | Symbol y :: t | String y :: t ->
                match System.Double.TryParse y with
                | (true, v) -> setStack (Number v :: t) s
                | _ -> failwith "Cannot cast to Number"
            | List l :: t -> setStack (Number (List.length l |> float) :: t) s
            | Map m :: t -> setStack (Number (Map.count m |> float) :: t) s
            | Word _ :: _ -> failwith "Word cannot be cast to Number value"
            | [] -> failwith "Stack underflow")

        primitive ">list" (fun s ->
            match getStack s with
            | Symbol y :: t | String y :: t -> setStack ((y |> Seq.toList |> List.map (string >> String) |> List) :: t) s
            | Map m :: t ->
                let listOfPairs (kv : KeyValuePair<string, Value>) = List [String kv.Key; kv.Value]
                setStack (List (Seq.map listOfPairs m |> Seq.toList) :: t) s
            | _ :: _ -> failwith "Expected s|y|m"
            | [] -> failwith "Stack underflow")

        primitive ">map" (fun s ->
            match getStack s with
            | List l :: t ->
                let pairOfList = function
                    | List kv -> match kv with String k :: v :: [] -> (k, v) | _ -> failwith "Expected key-value pair"
                    | _ -> failwith "Expected List of key-value pairs"
                setStack (Map (l |> Seq.map pairOfList |> Map.ofSeq) :: t) s
            | _ :: _ -> failwith "Expected l"
            | [] -> failwith "Stack underflow")

        primitive ">num?" (fun s ->
            match getStack s with
            | Symbol y :: t | String y :: t ->
                match System.Double.TryParse y with
                | (true, v) -> setStack (Number -1. :: Number v :: t) s
                | _ -> setStack (Number 0. :: t) s
            | t -> setStack (Number 0. :: t) s)

        primitive ">utf8" (fun s ->
            match getStack s with
            | String y :: t ->
                setStack (List (Encoding.UTF8.GetBytes(y) |> Seq.map (double >> Number) |> Seq.toList) :: t) s
            | _ :: _ -> failwith "Expected s"
            | [] -> failwith "Stack underflow")

        primitive "utf8>" (fun s ->
            match getStack s with
            | List b :: t -> 
                let bytes = b |> List.map (function Number n -> byte n | _ -> failwith "Expected list of n") |> List.toArray
                let str = Encoding.UTF8.GetString(bytes)
                setStack (String str :: t) s
            | _ :: _ -> failwith "Expected List"
            | [] -> failwith "Stack underflow")

        primitive ">ieee754" (fun s ->
            match getStack s with
            | Number n :: t ->
                setStack (List (BitConverter.GetBytes(n) |> Seq.map (double >> Number) |> Seq.toList) :: t) s
            | _ :: _ -> failwith "Expected n"
            | [] -> failwith "Stack underflow")

        primitive "ieee754>" (fun s ->
            match getStack s with
            | List b :: t -> 
                if b.Length <> 8 then failwith "Expected list of length 8"
                let bytes = b |> List.map (function Number n -> byte n | _ -> failwith "Expected list of n") |> List.toArray
                let n = BitConverter.ToDouble(bytes, 0);
                setStack (Number n :: t) s
            | _ :: _ -> failwith "Expected List"
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
            | _ :: _ :: _ -> failwith "Expected l"
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

        primitive "get-state" (fun s -> setStack (Map s :: getStack s) s)

        primitive "get-continuation" (fun s ->
            let continuation =
                match Map.find _continuation s with
                | List _ as c -> c
                | _ -> failwith "Expected List continuation"
            setStack (continuation :: getStack s) s)

        primitive "set-state" (fun s ->
            match getStack s with
            | Map s' :: _ -> s'
            | _ :: _ -> failwith "Expected m"
            | _ -> failwith "Stack underflow")

#if DEBUG
        // reimplemented in serdes.b
        primitive "serialize" (fun s ->
            printfn "!!! F# Serialize !!!"
            match getStack s with
            | x :: t ->
                use mem = new MemoryStream()
                use writer = new BinaryWriter(mem)
                serialize writer x
                let r = mem.ToArray()
                setStack (List (Array.map (fun b -> b |> double |> Number) r |> Seq.toList) :: t) s
            | _ -> failwith "Stack underflow")
#endif

        primitive "deserialize" (fun s ->
            printfn "!!! F# Deserialize !!!"
            match getStack s with
            | List b :: t ->
                let r = b |> List.map (function Number n -> byte n | _ -> failwith "Expected list of n") |> List.toArray
                use mem = new MemoryStream(r)
                use reader = new BinaryReader(mem)
                let primMap = primitives |> Seq.map (fun p -> p.Name, Word p) |> Map.ofSeq
                let x = deserialize primMap reader
                setStack (x :: t) s
            | _ :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")

        primitive "save" (fun s ->
            match getStack s with
            | String n :: List b :: t ->
                let s' = setStack t s
                use file = File.OpenWrite(n)
                let r = b |> List.map (function Number n -> byte n | _ -> failwith "Expected list of n") |> List.toArray
                file.Write(r, 0, r.Length)
                s'
            | _ :: _ :: _ -> failwith "Expected s r"
            | _ -> failwith "Stack underflow")

        primitive "load" (fun s ->
            match getStack s with
            | String n :: t ->
                use file = File.OpenRead(n)
                let len = int file.Length
                let r = Array.create<byte> len 0uy
                file.Read(r, 0, len) |> ignore
                setStack (List (Array.map (fun b -> b |> double |> Number) r |> Seq.toList) :: t) s
            | _ :: _ -> failwith "Expected s"
            | _ -> failwith "Stack underflow")
    ]
    
let primitiveState = addPrimitives emptyState primitives
