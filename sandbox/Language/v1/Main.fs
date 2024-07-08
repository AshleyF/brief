module MainV1

open System

type Value =
    | String  of string
    | Number  of double
    | Symbol  of string
    | List    of Value list
    | Map     of Map<string, Value>
    | Word    of (State -> State) 
    | Quote   of Value list

and State = {
    Stack: Value list
    Define: (string * Value list) list
    Dictionary: Map<string, Value> list
    Continuation: Value list }

let lex source =
    let concat = Seq.rev >> Seq.map char >> String.Concat
    let prepend token tokens = if Seq.length token > 0 then (concat token :: tokens) else tokens
    let unescape = function 'b' -> '\b' | 'f' -> '\f' | 'n' -> '\n' | 'r' -> '\r' | 't' -> '\t' | c -> c
    let rec str tokens token = function
        | '\\' :: c :: cs -> str tokens (unescape c :: token) cs
        | '"' :: cs -> lex' (concat token :: tokens) [] cs
        | c :: cs -> str tokens (c :: token) cs
        | [] -> failwith "Incomplete string"
    and lex' tokens token = function
        | c :: cs when Char.IsWhiteSpace c -> lex' (prepend token tokens) [] cs
        | '"' :: cs when Seq.length token = 0 -> str tokens ['\''] cs // prefix token with '
        //| '[' :: cs -> lex' ("[" :: (prepend token tokens)) [] cs
        //| ']' :: cs -> lex' ("]" :: (prepend token tokens)) [] cs
        | c :: cs -> lex' tokens (c :: token) cs
        | [] -> prepend token tokens
    source |> List.ofSeq |> lex' [] []

let parse tokens =
    let rec parse' values = function
        | (t : string) :: ts -> // TODO: why type annotation?
            if t.StartsWith '\'' then parse' (String (t.Substring 1) :: values) ts
            else
                match Double.TryParse t with
                | (true, n) -> parse' (Number n :: values) ts
                | _ -> parse' (Symbol t :: values) ts
        | [] -> values
    parse' [] tokens

let push value state = { state with Stack = value :: state.Stack }
let cont code state = { state with Continuation = code }
let tryFind name state =
    let rec tryFind' = function
        | dict :: ds ->
            match Map.tryFind name dict with
            | Some v -> Some v
            | None -> tryFind' ds
        | [] -> None
    tryFind' state.Dictionary
let rec interpret (state : State) =
    printfn "INTERPRET: %A" state
    match state.Continuation with
    | (String _) | (Number _) | (List _) | (Map _) | (Quote _) as v :: ws ->
        state |> push v |> cont ws |> interpret
    | Symbol s :: vs ->
        let handle s =
            match state.Define with
            | (n, d) :: ds -> { state with Define = (n, Symbol s :: d) :: ds } |> cont vs |> interpret
            | [] ->
                match tryFind s state with
                | Some v -> state |> cont (v :: vs) |> interpret
                | None -> failwith $"Unknown symbol '{s}"
        if s.Length > 1 then
            match s[0] with
            | ':' -> { state with Define = (s.Substring(1), []) :: state.Define } |> cont vs |> interpret
            | _ -> handle s
        else handle s
    | Word w :: vs -> state |> cont vs |> w |> interpret
    | [] -> state

let main() =
    let mult state =
        match state.Stack with
        | Number x :: Number y :: vs -> { state with Stack = Number (x * y) :: vs }
        | _ -> failwith "TODO" // TODO
    let primitives = Map.ofSeq [
        ("*", Word mult)]
    let state = { Stack = []; Define = []; Dictionary = [primitives]; Continuation = [] }
    let code =
        "3 4 5 * * " +
        ":foo 42 7 "
        //"  th\"is 'is   [ a \"good \\\"test\"  [ ] [ 3 4 ]   * ]  "
        |> lex |> parse
    { state with Continuation = code } |> interpret |> printfn "Test: %A"
