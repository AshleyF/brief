printfn "Hello WASM World!"

open System
open Utility
open Wasm
open Tests

testAll ()
save (Seq.rev [0uy; 97uy; 115uy; 109uy; 1uy; 0uy; 0uy; 0uy; 1uy; 5uy; 1uy; 96uy; 0uy; 1uy; 127uy; 3uy; 2uy; 1uy; 0uy; 5uy; 3uy; 1uy; 0uy; 1uy; 7uy; 17uy; 2uy; 6uy; 109uy; 101uy; 109uy; 111uy; 114uy; 121uy; 2uy; 0uy; 4uy; 109uy; 97uy; 105uy; 110uy; 0uy; 0uy; 10uy; 10uy; 1uy; 132uy; 128uy; 128uy; 128uy; 0uy; 0uy; 65uy; 42uy; 11uy])