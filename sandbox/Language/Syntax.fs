module Syntax

open System
open Structure

let lex source =
    let rec lex' token str source = seq {
        let emit (token: char list) = seq { if List.length token > 0 then yield token |> List.rev |> String.Concat }
        if str then
            match source with
            | '\\' :: '"' :: t -> yield! lex' ('"' :: token) true t
            | '"' :: t -> 
                yield! emit token
                yield! lex' [] false t
            | c :: t -> yield! lex' (c :: token) true t
            | [] -> failwith "Incomplete string"
        else
            match source with
            | c :: t when Char.IsWhiteSpace c ->
                yield! emit token
                yield! lex' [] false t
            | ('[' as c) :: t | (']' as c) :: t | ('{' as c) :: t | ('}' as c) :: t ->
                yield! emit token
                yield c.ToString()
                yield! lex' [] false t
            | '"' :: t -> yield! lex' ['\''] true t // prefix token with '
            | c :: t -> yield! lex' (c :: token) false t
            | [] -> yield! emit token }
    source |> List.ofSeq |> lex' [] false |> Seq.rev

type Node =
    | Token of string
    | Quote of Node list
    | Pairs of (string * Node) list

let stripLeadingTick s = if String.length s > 1 then s.Substring(1) else ""

let parse tokens =
    let rec parse' level nodes tokens =
        match tokens with
        | "]" :: t ->
            let q, t' = parse' (level + 1) [] t
            parse' level (Quote (List.rev q) :: nodes) t'
        | "[" :: t -> if level <> 0 then List.rev nodes, t else failwith "Unexpected quotation open"
        | "}" :: t ->
            let m, t' = parse' (level + 1) [] t
            let rec pairs list = seq {
                match list with
                | Token n :: v :: t ->
                    if n.StartsWith '\'' then yield stripLeadingTick n, v
                    else failwith "Expected string name"
                    yield! pairs t
                | [] -> ()
                | _ -> failwith "Expected name/value pair" }
            parse' level (Pairs (m |> List.rev |> pairs |> List.ofSeq) :: nodes) t'
        | "{" :: t -> if level <> 0 then List.rev nodes, t else failwith "Unexpected map open"
        | [] -> if level = 0 then List.rev nodes, [] else failwith "Unmatched quotation or map syntax"
        | token :: t -> parse' level (Token token :: nodes) t
    match tokens |> List.ofSeq |> parse' 0 [] with
    | (result, []) -> result
    | _ -> failwith "Unmatched quotation or map syntax"

let rec compile nodes =
    let rec compile' node =
        match node with
        | Token t ->
            match Double.TryParse t with
            | (true, v) -> Number v
            | _ ->
                match Boolean.TryParse t with
                | (true, v) -> Boolean v
                | _ -> if t.StartsWith '\'' then stripLeadingTick t |> String else Symbol t
        | Quote q -> List (compile q |> List.ofSeq)
        | Pairs p -> p |> Seq.map (fun (n, v) -> n, compile' v) |> Map.ofSeq |> Map
    nodes |> Seq.map compile'

let brief source = source |> lex |> parse |> compile |> List.ofSeq
