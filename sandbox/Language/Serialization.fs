module Serialization

open Structure

type Token =
    | Sym  of string
    | Num  of double
    | Str  of string
    | Bool of bool

let rec tokensOfValue v = seq {
    match v with
    | Symbol    s -> yield Sym s
    | Number    n -> yield Num n
    | String    s -> yield Str s
    | Boolean   b -> yield Bool b
    | List l ->
        yield Sym "list"
        for v in List.rev l do
            yield! tokensOfValue v
            yield Sym "cons"
    | Map m ->
        yield Sym "map"
        for kv in m do
            yield! tokensOfValue kv.Value
            yield Str kv.Key
            yield Sym "add" }

let bytesOfTokens tokens = seq {
    match Seq.tryHead tokens with
    | Some token ->
        match token with
        | Sym s -> 0uy
        | Num n -> 0uy
        | Str n -> 0uy
        | Bool n -> 0uy
    | None -> () }
