module Lisp

open System
open System.Numerics

type Token =
    | Open | Close
    | Number of string
    | Symbol of string
    | String of string

let tokenize source =
    let rec symbol acc = function
        | (')' :: _) as t -> acc, t // closing paren terminates
        | w :: t when Char.IsWhiteSpace(w) -> acc, t // whitespace terminates
        | [] -> acc, [] // end of list terminates
        | c :: t -> symbol (acc + (c.ToString())) t // otherwise accumulate chars
    let rec string acc = function 
        | '\\' :: '"' :: t -> string (acc + "\"") t // escaped quote becomes quote 
        | '"' :: t -> acc, t // closing quote terminates 
        | c :: t -> string (acc + (c.ToString())) t // otherwise accumulate chars 
        | _ -> failwith "Malformed string." 
    let rec token acc = function 
        | (')' :: _) as t -> acc, t // closing paren terminates 
        | w :: t when Char.IsWhiteSpace(w) -> acc, t // whitespace terminates 
        | [] -> acc, [] // end of list terminates 
        | c :: t -> token (acc + (c.ToString())) t // otherwise accumulate chars 
    let rec tokenize' acc = function
        | w :: t when Char.IsWhiteSpace(w) -> tokenize' acc t // skip whitespace
        | '(' :: t -> tokenize' (Open :: acc) t
        | ')' :: t -> tokenize' (Close :: acc) t
        | '"' :: t -> // start of string 
            let s, t' = string "" t 
            tokenize' (Token.String(s) :: acc) t' 
        | '-' :: d :: t when Char.IsDigit(d) -> // start of negative number
            let n, t' = token ("-" + d.ToString()) t
            tokenize' (Token.Number(n) :: acc) t'
        | d :: t when Char.IsDigit(d) -> // start of positive number
            let n, t' = token (d.ToString()) t
            tokenize' (Token.Number(n) :: acc) t'
        | s :: t -> // otherwise start of symbol
            let s, t' = symbol (s.ToString()) t
            tokenize' (Token.Symbol(s) :: acc) t'
        | [] -> List.rev acc // end of list terminates
    tokenize' [] source

type Expression =
    | Number of int // TODO: bigint
    | Symbol of string
    | String of string
    | List of Expression list

let parse source =
    let map = function
        | Token.Number(n) -> Expression.Number(Int32.Parse(n)) // TODO: bigint
        | Token.Symbol(s) -> Expression.Symbol(s)
        | Token.String(s) -> Expression.Symbol(s)
        | _ -> failwith "Syntax error."
    let rec parse' acc = function
        | Open :: t ->
            let e, t' = parse' [] t
            parse' (List(e) :: acc) t'
        | Close :: t -> (List.rev acc), t
        | h :: t -> parse' ((map h) :: acc) t
        | [] -> (List.rev acc), []
    let result, _ = parse' [] source
    result

let rec print = function 
    | String(s) -> 
        let escape = String.collect (function '"' -> "\\\"" | c -> c.ToString()) // escape quotes 
        "\"" + (escape s) + "\"" 
    | Symbol(s) -> s 
    | Number(n) -> n.ToString() 
    | List(list) -> "(" + String.Join(" ", (List.map print list)) + ")" 

let rec eval expression = 
    printfn "Redex: %s" (print expression)
    let expression' =
        match expression with 
        | List([Number(0); e; Symbol("+")]) -> e // (0 e +) -> e
        | List([e; Number(0); Symbol("+")]) -> e // (e 0 +) -> e
        | List([Number(n); e; Symbol("-")]) when n < 0 -> List([Number(-n); e; Symbol("+")]) // (e -n -) -> (e n +)
        | List([e; Number(0); Symbol("-")]) -> e // (e 0 -) -> e
        | List([Number(0); e; Symbol("*")]) -> Number(0) // (0 e *) -> 0
        | List([e; Number(0); Symbol("*")]) -> Number(0) // (e 0 *) -> 0
        | List([Number(1); e; Symbol("*")]) -> e // (1 e *) -> e
        | List([e; Number(1); Symbol("*")]) -> e // (e 1 *) -> e
        | List([e; Number(1); Symbol("/")]) -> e // (e 1 /) -> e
        | List([e0; e1; Symbol("eq?")]) -> Symbol(if eval e0 = eval e1 then "#t" else "#f") // (e0 e1 eq?) -> #t/#f
        | List([e0; e1; Symbol("#t"); Symbol("if")]) -> e0 // (e0 e1 #t if) -> e0
        | List([e0; e1; Symbol("#f"); Symbol("if")]) -> e1 // (e0 e1 #f if) -> e1
        | List([e0; e1; p; Symbol("if")]) -> List([e0; e1; eval p; Symbol("if")]) // (e0 e1 p if) -> eval predicate
        | Number(_) as n -> n
        | String(_) as s -> s
        | Symbol(_) as s -> s
        | List(_)   as l -> l
    if expression' <> expression then eval expression' else expression

let rep = List.ofSeq >> tokenize >> parse >> List.head >> eval >> print

let rec repl output = 
    printf "%s\n> " output 
    try Console.ReadLine() |> rep |> repl 
    with ex -> repl ex.Message

repl "Welcome to Brief Lazy Lisp"