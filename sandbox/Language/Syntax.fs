module Syntax

open System
open Structure

let lex source =
    let rec lex' token source = seq {
        let emit (token: char list) = seq { if List.length token > 0 then yield token |> List.rev |> String.Concat }
        match source with
        | c :: t when Char.IsWhiteSpace c ->
            yield! emit token
            yield! lex' [] t
        | ('[' as c) :: t | (']' as c) :: t ->
            yield! emit token
            yield c.ToString()
            yield! lex' [] t
        | c :: t -> yield! lex' (c :: token) t
        | [] -> yield! emit token }
    source |> List.ofSeq |> lex' []

type Node =
    | Token of string
    | Quote of Node list

let parse tokens =
    let rec parse' nodes tokens =
        match tokens with
        | "[" :: t ->
            let q, t' = parse' [] t
            parse' (Quote q :: nodes) t'
        | "]" :: t -> List.rev nodes, t
        | [] -> List.rev nodes, []
        | token :: t -> parse' (Token token :: nodes) t
    match tokens |> List.ofSeq |> parse' [] with
    | (result, []) -> result
    | _ -> failwith "Unexpected quotation close"

let rec compile (dictionary: Map<string, Word>) nodes = seq {
    match nodes with
    | Token t :: n ->
        match Map.tryFind t dictionary with
        | Some w -> yield w
        | None ->
            match Double.TryParse t with
            | (true, v) -> yield Literal (Number v)
            | _ ->
                match Boolean.TryParse t with
                | (true, v) -> yield Literal (Boolean v)
                | _ ->
                    if t.StartsWith '\'' && t.Length > 1
                    then yield Literal (String (t.Substring(1)))
                    else failwith (sprintf "Unknown word: %s" t)
        yield! compile dictionary n
    | Quote q :: n ->
        yield Literal (Quotation (compile dictionary n |> List.ofSeq))
        yield! compile dictionary n
    | [] -> () }

let brief dictionary = lex >> parse >> compile dictionary >> List.ofSeq
