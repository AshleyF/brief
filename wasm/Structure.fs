module Structure

type Value = I32 | I64 | F32 | F64 // value_type

type FuncType = { // func_type
    Parameters: Value seq
    Returns: Value option } // future multiple supported: http://webassembly.org/docs/future-features/#multiple-return

type ImportName = {
    Module: string
    Field: string }

type ResizableLimits = int * int option // initial * maximum

type ImportEntry =
    | Function of ImportName * int
    | Table of ImportName * ResizableLimits // currently only anyfunc supported
    | Memory of ImportName * ResizableLimits
    | Global of ImportName * Value * bool

type ExternalKind = Function | Table | Memory | Global

type ExportEntry = {
    Field: string
    Kind: ExternalKind
    Index: int }

type Instruction = // using int rather than uint32 throughout for convenience
    // control flow
    | Unreachable
    | Nop
    | Block of Value option // block type/empty
    | Loop of Value option // block type/empty
    | If of Value option // block type/empty
    | Else
    | End
    | Br of int // relative depth
    | BrIf of int // relative depth
    | BrTable of int * int seq * int // count, table, default
    | Return
    // call
    | Call of int // function index
    | CallIndirect of int // type index, (reserved future use)
    // parametric
    | Drop
    | Select
    // variable
    | GetLocal of int // index
    | SetLocal of int // index
    | TeeLocal of int // index
    | GetGlobal of int // index
    | SetGlobal of int // index
    // memory
    | LoadI32 of int * int // flags, offset
    | LoadI64 of int * int // flags, offset
    | LoadF32 of int * int // flags, offset
    | LoadF64 of int * int // flags, offset
    | LoadByteI32 of int * int // flags, offset
    | LoadByteUnsignedI32 of int * int // flags, offset
    | LoadShortI32 of int * int // flags, offset
    | LoadShortUnsignedI32 of int * int // flags, offset
    | LoadByteI64 of int * int // flags, offset
    | LoadByteUnsignedI64 of int * int // flags, offset
    | LoadShortI64 of int * int // flags, offset
    | LoadShortUnsignedI64 of int * int // flags, offset
    | LoadIntI64 of int * int // flags, offset
    | LoadIntUnsignedI64 of int * int // flags, offset
    | StoreI32 of int * int // flags, offset
    | StoreI64 of int * int // flags, offset
    | StoreF32 of int * int // flags, offset
    | StoreF64 of int * int // flags, offset
    | StoreByteI32 of int * int // flags, offset
    | StoreShortI32 of int * int // flags, offset
    | StoreByteI64 of int * int // flags, offset
    | StoreShortI64 of int * int // flags, offset
    | StoreIntI64 of int * int // flags, offset
    | CurrentMemory // reserved (future)
    | GrowMemory // reserved (future)
    // constants
    | ConstI32 of int
    | ConstI64 of int64
    | ConstF32 of single
    | ConstF64 of double
    // comparison
    | EqualZeroI32 // eqz
    | EqualI32 // eq
    | NotEqualI32 // neq
    | LessI32 // lt_s
    | LessUnsignedI32 // lt_u
    | GreaterI32 // gt_s
    | GreaterUnsignedI32 // gt_u
    | LessOrEqualI32 // le_s
    | LessOrEqualUnsignedI32 // le_u
    | GreaterOrEqualI32 // ge_s
    | GreaterOrEqualUnsignedI32 // ge_u
    // ----------------------------------------
    | EqualZeroI64 // eqz
    | EqualI64 // eq
    | NotEqualI64 // neq
    | LessI64 // lt_s
    | LessUnsignedI64 // lt_u
    | GreaterI64 // gt_s
    | GreaterUnsignedI64 // gt_u
    | LessOrEqualI64 // le_s
    | LessOrEqualUnsignedI64 // le_u
    | GreaterOrEqualI64 // ge_s
    | GreaterOrEqualUnsignedI64 // ge_u
    // ----------------------------------------
    | EqualF32 // eq
    | NotEqualF32 // neq
    | LessF32 // lt_s
    | GreaterF32 // gt_s
    | LessOrEqualF32 // le_s
    | GreaterOrEqualF32 // ge_s
    // ----------------------------------------
    | EqualF64 // eq
    | NotEqualF64 // neq
    | LessF64 // lt_s
    | GreaterF64 // gt_s
    | LessOrEqualF64 // le_s
    | GreaterOrEqualF64 // ge_s
    // numeric
    | ClzI32 // i32.clz
    | CtzI32 // i32.ctz
    | PopCountI32 // i32.popcnt
    | AddI32 // i32.add
    | SubI32 // i32.sub
    | MulI32 // i32.mul
    | DivI32 // i32.div_s
    | DivUnsignedI32 // i32.div_u
    | RemI32 // i32.rem_s
    | RemUnsignedI32 // i32.rem_u
    | AndI32 // i32.and
    | OrI32 // i32.or
    | XorI32 // i32.xor
    | ShiftLeftI32 // i32.shl
    | ShiftRightI32 // i32.shr_s
    | ShiftRightUnsignedI32 // i32.shr_u
    | RotateLeftI32 // i32.rotl
    | RotateRightI32 // i32.rotr
    // ----------------------------------------
    | ClzI64 // i64.clz
    | CtzI64 // i64.ctz
    | PopCountI64 // i64.popcnt
    | AddI64 // i64.add
    | SubI64 // i64.sub
    | MulI64 // i64.mul
    | DivI64 // i64.div_s
    | DivUnsignedI64 // i64.div_u
    | RemI64 // i64.rem_s
    | RemUnsignedI64 // i64.rem_u
    | AndI64 // i64.and
    | OrI64 // i64.or
    | XorI64 // i64.xor
    | ShiftLeftI64 // i64.shl
    | ShiftRightI64 // i64.shr_s
    | ShiftRightUnsignedI64 // i64.shr_u
    | RotateLeftI64 // i64.rotl
    | RotateRightI64 // i64.rotr
    // ----------------------------------------
    | AbsF32 // f32.abs
    | NegateF32 // f32.neg
    | CeilingF32 // f32.ceil
    | FloorF32 // f32.floor
    | TruncateF32 // f32.trunc
    | NearestF32 // f32.nearest
    | SqrtF32 // f32.sqrt
    | AddF32 // f32.add
    | SubF32 // f32.sub
    | MulF32 // f32.mul
    | DivF32 // f32.div
    | MinF32 // f32.min
    | MaxF32 // f32.max
    | CopySignF32 // f32.copysign
    // ----------------------------------------
    | AbsF64 // f64.abs
    | NegateF64 // f64.neg
    | CeilingF64 // f64.ceil
    | FloorF64 // f64.floor
    | TruncateF64 // f64.trunc
    | NearestF64 // f64.nearest
    | SqrtF64 // f64.sqrt
    | AddF64 // f64.add
    | SubF64 // f64.sub
    | MulF64 // f64.mul
    | DivF64 // f64.div
    | MinF64 // f64.min
    | MaxF64 // f64.max
    | CopySignF64 // f64.copysign
    // conversions
    | WrapI64asI32 // i32.wrap_i64
    | TruncateF32asI32 // i32.trunc_s_f32
    | TruncateUnsignedF32asI32 // i32.trunc_u_f32
    | TruncateF64asI32 // i32.trunc_s_f64
    | TruncateUnsignedF64asI32 // i32.trunc_u_f64
    | ExtendI32toI64 // i64.extend_s_i32
    | ExtendUnsignedI32toI64 // i64.extend_u_i32
    | TruncateF32asI64 // i64.trunc_s_f32
    | TruncateUnsignedF32asI64 // i64.trunc_u_f32
    | TruncateF64asI64 // i64.trunc_s_f64
    | TruncateUnsignedF64asI64 // i64.trunc_u_f64
    | ConvertI32toF32 // f32.convert_s_i32
    | ConvertUnsignedI32toF32 // f32.convert_u_i32
    | ConvertI64toF32 // f32.convert_s_i64
    | ConvertUnsignedI64toF32 // f32.convert_u_i64
    | DemoteF64toF32 // f32.demote_f64
    | ConvertI32toF64 // f64.convert_s_i32
    | ConvertUnsignedI32toF64 // f64.convert_u_i32
    | ConvertI64toF64 // f64.convert_s_i64
    | ConvertUnsignedI64toF64 // f64.convert_u_i64
    | PromoteF32toF64 // f64.promote_f32
    // reinterpretations
    | ReinterpretF32asI32 // i32.reinterpret_f32
    | ReinterpretF64asI64 // i64.reinterpret_f64
    | ReinterpretI32asF32 // f32.reinterpret_i32
    | ReinterpretI64asF64 // f64.reinterpret_i64

type GlobalVariable = {
    Value: Value
    Mutable: bool
    Init: Instruction seq }

type LocalEntry = {
    Number: int
    Type: Value }

type FunctionBody = {
    Locals: LocalEntry seq
    Code: Instruction seq }

type ElementSegment = {
    Index: int // 0 in MVP
    Offset: Instruction seq // init expression computing offset
    Elements: int seq } // function indices

type DataSegment = { // data_segment
    Index: int
    Offset: Instruction seq // init expression computing offset
    Data: byte seq }

type Section =
    | Todo of byte * byte seq
    | Type of FuncType seq
    | Import of ImportEntry seq
    | Function of int seq // indices into Types
    | Table of ResizableLimits
    | Memory of ResizableLimits
    | Global of GlobalVariable seq
    | Export of ExportEntry seq
    | Start of int // function index
    | Element of ElementSegment seq
    | Code of FunctionBody seq
    | Data of DataSegment seq
    | Custom of string * byte seq

type Module = Section seq