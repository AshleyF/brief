module Tests

open Wasm

let test name result expected =
    let result' = result |> Seq.toList
    printfn "%s -> %A" name result'
    if result' <> expected then failwithf "Test failure (expected %A)" expected

let testfail name fn =
    printfn "%s -> failure (as expected)" name
    try fn () |> ignore; failwith "Test failure (expected failure)" with _-> ()

let testVarInt () =
    test "Type varuint1 0" (varuint1 0) [0uy]
    test "Type varuint1 1" (varuint1 1) [1uy]
    testfail "Type varuint1 -1" (fun () -> varuint1 -1)
    testfail "Type varuint1 2" (fun () -> varuint1 2)

    test "Type varuint7 127" (varuint7 127) [127uy]
    testfail "Type varuint7 -1" (fun () -> varuint7 -1)
    testfail "Type varuint7 128" (fun () -> varuint7 128)

    test "Type varuint32 123" (varuint32 123u) [123uy]
    test "Type varuint32 456" (varuint32 456u) [200uy; 3uy]
    test "Type varuint32 12345" (varuint32 12345u) [185uy; 96uy]
    test "Type varuint32 624485" (varuint32 624485u) [229uy; 142uy; 38uy]
    test "Type varuint32 3000000000" (varuint32 3000000000u) [128uy; 188uy; 193uy; 150uy; 11uy]

    test "Type varint7 1" (varint7 1) [1uy]
    test "Type varint7 127" (varint7 127) [255uy; 0uy]
    test "Type varint7 -1" (varint7 -1) [127uy]
    test "Type varint7 -128" (varint7 -128) [128uy; 127uy]
    testfail "Type varint7 128" (fun () -> varint7 128)

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
    test "Global import entry" (importEntry (ImportEntry.Global ({ Module = "abc"; Field = "def"}, I32, true))) [3uy; 97uy; 98uy; 99uy; 3uy; 100uy; 101uy; 102uy; 3uy; 127uy; 1uy]

let testSections () =
    test "Section" (section 11uy [1uy; 2uy; 3uy]) [11uy; 3uy; 1uy; 2uy; 3uy]
    test "Type section" (typeSection [{ Parameters = []; Returns = Some I32 }]) [1uy; 5uy; 1uy; 96uy; 0uy; 1uy; 127uy]
    test "Import section" (importSection [ImportEntry.Function ({ Module = "abc"; Field = "def"}, 123); ImportEntry.Memory ({ Module = "abc"; Field = "def"}, (10, None))]) [2uy; 22uy; 2uy; 3uy; 97uy; 98uy; 99uy; 3uy; 100uy; 101uy; 102uy; 0uy; 123uy; 3uy; 97uy; 98uy; 99uy; 3uy; 100uy; 101uy; 102uy; 2uy; 0uy; 10uy]
    test "Function section" (functionSection [1; 2; 3]) [3uy; 4uy; 3uy; 1uy; 2uy; 3uy]
    test "Table section (no max)" (tableSection (10, None)) [4uy; 4uy; 1uy; 112uy; 0uy; 10uy]
    test "Table section (with max)" (tableSection (10, Some 20)) [4uy; 5uy; 1uy; 112uy; 1uy; 10uy; 20uy]
    test "Custom section" (customSection "test" [1uy; 2uy; 3uy]) [0uy; 8uy; 4uy; 116uy; 101uy; 115uy; 116uy; 1uy; 2uy; 3uy]

let testAll () =
    printfn "Running tests..."
    testVarInt ()
    testHeader ()
    testResizableLimits ()
    testImportEntry ()
    testSections ()