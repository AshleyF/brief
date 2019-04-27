printfn "Hello WASM World!"

open Utility
open Structure
open Encoding
open Tests

testAll ()

printfn "Building module..."

let bytes =
    wasm [
        Type [{ Parameters = []; Returns = Some Value.I32 }]
        Function [0]
        Memory (1, None)
        Export [{ Field = "memory"; Kind = ExternalKind.Memory; Index = 0 }; { Field = "main"; Kind = ExternalKind.Function; Index = 0 }]
        Code [{ Locals = []; Code = [ConstI32 8; ConstI32 6; MulI32; End] }]
    ]

save bytes