module Syntax

open System
open Structure

let lex source =
    let emit (token: char list) = seq { if List.length token > 0 then yield token |> List.rev |> String.Concat }
    let rec unescape = function 'b' -> '\b' | 'f' -> '\f' | 'n' -> '\n' | 'r' -> '\r' | 't' -> '\t' | c -> c
    and tick token source = seq {
        match source with
        | '\\' :: c :: t -> yield! tick (unescape c :: token) t
        | ((']' :: _) as t) | (('[' :: _) as t) | (('}' :: _) as t) | (('{' :: _) as t) ->
            yield! emit token
            yield! lex' [] t
        | c :: t when Char.IsWhiteSpace c ->
            yield! emit token
            yield! lex' [] t
        | c :: t -> yield! tick (c :: token) t
        | [] -> yield! emit token }
    and str token source = seq {
        match source with
        | '\\' :: c :: t -> yield! str (unescape c :: token) t
        | '"' :: t  -> 
            if token.Length > 0 then yield! emit token
            yield! lex' [] t
        | c :: t -> yield! str (c :: token) t
        | [] -> failwith "Incomplete string" }
    and lex' token source = seq {
        match source with
        | c :: t when Char.IsWhiteSpace c ->
            if token.Length > 0 then yield! emit token
            yield! lex' [] t
        | ('[' as c) :: t | (']' as c) :: t | ('{' as c) :: t | ('}' as c) :: t ->
            if token.Length > 0 then yield! emit token
            yield c.ToString()
            yield! lex' [] t
        | ('\'' :: _ as t) when token.Length = 0 -> yield! tick [] t
        | '"' :: t when token.Length = 0 -> yield! str ['\''] t // prefix token with '
        | c :: t -> yield! lex' (c :: token) t
        | [] -> yield! emit token }
    source |> List.ofSeq |> lex' [] |> Seq.rev

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
            | _ -> if t.StartsWith '\'' then stripLeadingTick t |> String else Symbol t
        | Quote q -> List (compile q |> List.ofSeq)
        | Pairs p -> p |> Seq.map (fun (n, v) -> n, compile' v) |> Map.ofSeq |> Map
    nodes |> Seq.map compile'

let brief source = source |> lex |> parse |> compile |> List.ofSeq
