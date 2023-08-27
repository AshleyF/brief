module Lisp

open System
open System.Numerics

type Token =
    | Open | Close
    | Number of string
    | String of string
    | Symbol of string

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
    | Number of bigint
    | String of string
    | Symbol of string
    | List of Expression list
    | Function of (Expression list -> Expression)

let parse source =
    let map = function
        | Token.Symbol(s) -> Expression.Symbol(s)
        | Token.Number(n) -> Expression.Number(bigint.Parse(n))
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

let Multiply args = 
    let prod a = function Number(b) -> a * b | _ -> failwith "Malformed multiplication argument."  
    Number(List.fold prod 1I args)

let rec If = function 
    | [condition; t; f] -> 
        match eval condition with 
        | List([_]) | Symbol("#t") -> eval t // non-empty list or #t is true 
        | List([]) | Symbol("#f") -> eval f // empty list or #f is false 
        | _ -> failwith "Unexpected truth value"
    | _ -> failwith "Malformed 'if'."

and environment = 
    Map.ofList [ 
        "#t", Symbol("#t")
        "#f", Symbol("#f")
        "*", Function(Multiply)
        "if", Function(If)]

and eval expression = 
    printfn $"EXP {expression:A}"
    match expression with 
    | String(_) as lit -> lit 
    | Symbol(s) -> environment.[s]  
    | Number(_) as lit -> lit 
    | List([]) -> List([]) 
    | List(h :: t) ->  
        match eval h with 
        | Function(f) -> apply f t 
        | _ -> failwith "Malformed expression." 
    | _ -> failwith "Malformed expression."
and apply fn args = fn (List.map eval args)

let rec print = function 
    | String(s) -> 
        let escape = String.collect (function '"' -> "\\\"" | c -> c.ToString()) // escape quotes 
        "\"" + (escape s) + "\"" 
    | Symbol(s) -> s 
    | Number(n) -> n.ToString() 
    | List(list) -> "(" + String.Join(" ", (List.map print list)) + ")" 
    | Function(_) -> "Function"

let rep = List.ofSeq >> tokenize >> parse >> List.head >> eval >> print

let rec repl output = 
    printf "%s\n> " output 
    try Console.ReadLine() |> rep |> repl 
    with ex -> repl ex.Message

repl "Welcome to Brief Lazy Lisp"