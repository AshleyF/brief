module Tests

open Structure
open Encoding

let test name result expected =
    let result' = result |> Seq.toList
    printfn "%s -> %A" name result'
    if result' <> expected then failwithf "Test failure (expected %A result %A)" expected result'

let testfail name fn =
    printfn "%s -> failure (as expected)" name
    try fn () |> ignore; failwith "Test failure (expected failure)" with _-> ()

let testVarInt () =
    test "Type varuint32 123" (varuint32 123u) [123uy]
    test "Type varuint32 456" (varuint32 456u) [200uy; 3uy]
    test "Type varuint32 12345" (varuint32 12345u) [185uy; 96uy]
    test "Type varuint32 624485" (varuint32 624485u) [229uy; 142uy; 38uy]
    test "Type varuint32 3000000000" (varuint32 3000000000u) [128uy; 188uy; 193uy; 150uy; 11uy]

    test "Type varint32 123456" (varint32 123456) [192uy; 196uy; 7uy]
    test "Type varint32 -624485" (varint32 -624485) [155uy; 241uy; 89uy]

    test "Type varint64 -3000000000" (varint64 -3000000000L) [128uy; 196uy; 190uy; 233uy; 116uy]
    test "Type varint64 3000000000" (varint64 3000000000L) [128uy; 188uy; 193uy; 150uy; 11uy]

let testHeader () =
    test "Module header" moduleHeader [0x00uy; 0x61uy; 0x73uy; 0x6duy; 0x01uy; 0x00uy; 0x00uy; 0x00uy]

let testResizableLimits () =
    test "Initial limit" (resizable (10, None)) [0uy; 10uy]
    test "Maximum limit" (resizable (20, (Some 100))) [1uy; 20uy; 100uy]

let testImportEntry () =
    test "Function import entry" (importEntry (ImportEntry.Function ({ Module = "abc"; Field = "def"}, 123))) [3uy; 97uy; 98uy; 99uy; 3uy; 100uy; 101uy; 102uy; 0uy; 123uy]
    test "Table (no max) import entry" (importEntry (ImportEntry.Table ({ Module = "abc"; Field = "def"}, (10, None)))) [3uy; 97uy; 98uy; 99uy; 3uy; 100uy; 101uy; 102uy; 1uy; 112uy; 0uy; 10uy]
    test "Table (with max) import entry" (importEntry (ImportEntry.Table ({ Module = "abc"; Field = "def"}, (10, Some 20)))) [3uy; 97uy; 98uy; 99uy; 3uy; 100uy; 101uy; 102uy; 1uy; 112uy; 1uy; 10uy; 20uy]
    test "Memory (no max) import entry" (importEntry (ImportEntry.Memory ({ Module = "abc"; Field = "def"}, (10, None)))) [3uy; 97uy; 98uy; 99uy; 3uy; 100uy; 101uy; 102uy; 2uy; 0uy; 10uy]
    test "Memory (with max) import entry" (importEntry (ImportEntry.Memory ({ Module = "abc"; Field = "def"}, (10, Some 20)))) [3uy; 97uy; 98uy; 99uy; 3uy; 100uy; 101uy; 102uy; 2uy; 1uy; 10uy; 20uy]
    test "Global import entry" (importEntry (ImportEntry.Global ({ Module = "abc"; Field = "def"}, Value.I32, true))) [3uy; 97uy; 98uy; 99uy; 3uy; 100uy; 101uy; 102uy; 3uy; 127uy; 1uy]

let testSections () =
    test "Section" (section 11uy [1uy; 2uy; 3uy]) [11uy; 3uy; 1uy; 2uy; 3uy]
    test "Type section" (typeSection [{ Parameters = []; Returns = Some Value.I32 }]) [1uy; 5uy; 1uy; 96uy; 0uy; 1uy; 127uy]
    test "Import section" (importSection [ImportEntry.Function ({ Module = "abc"; Field = "def"}, 123); ImportEntry.Memory ({ Module = "abc"; Field = "def"}, (10, None))]) [2uy; 22uy; 2uy; 3uy; 97uy; 98uy; 99uy; 3uy; 100uy; 101uy; 102uy; 0uy; 123uy; 3uy; 97uy; 98uy; 99uy; 3uy; 100uy; 101uy; 102uy; 2uy; 0uy; 10uy]
    test "Function section" (functionSection [1; 2; 3]) [3uy; 4uy; 3uy; 1uy; 2uy; 3uy]
    test "Table section (no max)" (tableSection (10, None)) [4uy; 4uy; 1uy; 112uy; 0uy; 10uy]
    test "Table section (with max)" (tableSection (10, Some 20)) [4uy; 5uy; 1uy; 112uy; 1uy; 10uy; 20uy]
    test "Memory section (no max)" (memorySection (10, None)) [5uy; 3uy; 1uy; 0uy; 10uy]
    test "Memory section (with max)" (memorySection (10, Some 20)) [5uy; 4uy; 1uy; 1uy; 10uy; 20uy]
    test "Global section" (globalSection [{ Value = Value.F32; Mutable = false; Init = [ConstF32 3.14f; End] }]) [6uy; 9uy; 1uy; 125uy; 0uy; 67uy; 195uy; 245uy; 72uy; 64uy; 11uy]
    test "Export section" (exportSection [{ Field = "memory"; Kind = ExternalKind.Memory; Index = 0 }; { Field = "main"; Kind = ExternalKind.Function; Index = 0 }]) [7uy; 17uy; 2uy; 6uy; 109uy; 101uy; 109uy; 111uy; 114uy; 121uy; 2uy; 0uy; 4uy; 109uy; 97uy; 105uy; 110uy; 0uy; 0uy]
    test "Start section" (startSection 123) [8uy; 1uy; 123uy]
    test "Element section" (elementSection [{ Index = 0; Offset = [ConstI32 123]; Elements = [456]}]) [9uy; 8uy; 1uy; 0uy; 65uy; 251uy; 0uy; 1uy; 200uy; 3uy]
    test "Code section" (codeSection [{ Locals = [{ Number = 2; Type = I32 }]; Code = [ConstI32 42; End]}]) [10uy; 8uy; 1uy; 6uy; 1uy; 2uy; 127uy; 65uy; 42uy; 11uy]
    test "Data section" (dataSection [{ Index = 0; Offset = [ConstI32 123]; Data = [1uy; 2uy; 3uy]}]) [11uy; 9uy; 1uy; 0uy; 65uy; 251uy; 0uy; 3uy; 1uy; 2uy; 3uy]
    test "Custom section" (customSection "test" [1uy; 2uy; 3uy]) [0uy; 8uy; 4uy; 116uy; 101uy; 115uy; 116uy; 1uy; 2uy; 3uy]

type LocalEntry = {
    Number: int
    Type: Value }

type FunctionBody = {
    Locals: LocalEntry seq
    Code: Instruction seq }
let testAll () =
    printfn "Running tests..."
    testVarInt ()
    testHeader ()
    testResizableLimits ()
    testImportEntry ()
    testSections ()