module Prelude

open System
open Structure

let primitive name fn = Primitive (NamedPrimitive (name, fn))

let store = primitive "store" (fun s ->
    match s.Stack with
    | String n :: v :: t -> { s with Stack = t; Map = Map.add n v s.Map }
    | _ :: _ :: t -> failwith "Expected vs"
    | _ -> failwith "Stack underflow")

let fetch = primitive "fetch" (fun s ->
    match s.Stack with
    | String n :: t ->
        match Map.tryFind n s.Map with
        | Some v -> { s with Stack = v :: t }
        | None -> failwith "Not found" // TODO return flag?
    | _ :: t -> failwith "Expected s"
    | _ -> failwith "Stack underflow")

let i = primitive "i" (fun s ->
    match s.Stack with
    | Quotation q :: t -> { s with Stack = t; Continuation = q @ s.Continuation }
    | _ :: t -> failwith "Expected q"
    | _ -> failwith "Stack underflow")

let depth = primitive "depth" (fun s ->
    { s with Stack = Number (double s.Stack.Length) :: s.Stack })

let clear = primitive "clear" (fun s -> { s with Stack = [] })

let dup = primitive "dup" (fun s ->
    match s.Stack with
    | x :: t -> { s with Stack = x :: x :: t }
    | _ -> failwith "Stack underflow")

let drop = primitive "drop" (fun s ->
    match s.Stack with
    | _ :: t -> { s with Stack = t }
    | _ -> failwith "Stack underflow")

let dip = primitive "dip" (fun s ->
    match s.Stack with
    | Quotation q :: v :: t -> { s with Stack = t; Continuation = q @ Literal v :: s.Continuation }
    | _ :: _ :: t -> failwith "Expected vq"
    | _ -> failwith "Stack underflow")

let unaryOp name op = primitive name (fun s ->
    match s.Stack with
    | Number x :: t -> { s with Stack = Number (op x) :: t }
    | _ :: t -> failwith "Expected n"
    | _ -> failwith "Stack underflow")

let binaryOp name op = primitive name (fun s ->
    match s.Stack with
    | Number x :: Number y :: t -> { s with Stack = Number (op x y) :: t }
    | _ :: _ :: t -> failwith "Expected nn"
    | _ -> failwith "Stack underflow")

let add = binaryOp "+" (+)
let sub = binaryOp "-" (+)
let mul = binaryOp "*" (*)
let div = binaryOp "/" (/)

let chs = unaryOp "chs" (fun n -> -n)
let recip = unaryOp "recip" (fun n -> 1. / n)
let abs = unaryOp "abs" (fun n -> abs n)

let pi = Secondary ("pi", [Literal (Number Math.PI)])
let e = Secondary ("e", [Literal (Number Math.E)])

let sq = Secondary ("sq", [dup; mul])
let area = Secondary ("area", [sq; pi; mul])

let define = Secondary ("def", [store])

let prelude = [
    Literal (Quotation [store]);  Literal (String "!");     define
    Literal (Quotation [fetch]);  Literal (String "@");     define
    Literal (Quotation [i]);      Literal (String "i");     define
    Literal (Quotation [depth]);  Literal (String "depth"); define
    Literal (Quotation [clear]);  Literal (String "clear"); define
    Literal (Quotation [dup]);    Literal (String "dup");   define
    Literal (Quotation [drop]);   Literal (String "drop");  define
    Literal (Quotation [dip]);    Literal (String "dip");   define
    Literal (Quotation [add]);    Literal (String "+");     define
    Literal (Quotation [sub]);    Literal (String "-");     define
    Literal (Quotation [mul]);    Literal (String "*");     define
    Literal (Quotation [div]);    Literal (String "/");     define
    Literal (Quotation [chs]);    Literal (String "chs");   define
    Literal (Quotation [recip]);  Literal (String "recip"); define
    Literal (Quotation [abs]);    Literal (String "abs");   define
    Literal (Quotation [Literal (Number Math.PI)]); Literal (String "pi");   define
    Literal (Quotation [Literal (Number Math.E)]);  Literal (String "e");    define
    Literal (Quotation [dup; mul]);                 Literal (String "sq");   define
    Literal (Quotation [sq; pi; mul]);              Literal (String "area"); define
]
