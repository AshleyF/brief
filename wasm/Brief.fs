module Brief

open Structure

let brief (source : string) =
    let pop = function _ :: stack -> stack | _ -> failwith "Stack underflow"
    let push v stack = v :: stack
    let rec compile stack (words : string list) = seq {
        let un (i32, i64, f32, f64) =
            let op =
                match stack with
                | I32 :: _ -> i32
                | I64 :: _ -> i64
                | F32 :: _ -> f32
                | F64 :: _ -> f64
                | [] -> failwith (sprintf "Stack underflow at '%s' word" words.Head)
            if op <> Nop then op else failwith (sprintf "Invalid stack at '%s' word (stack: %A)" words.Head stack)
        let bin (i32, i64, f32, f64) =
            let op =
                match stack with
                | I32 :: I32 :: _ -> i32
                | I64 :: I64 :: _ -> i64
                | F32 :: F32 :: _ -> f32
                | F64 :: F64 :: _ -> f64
                | _ -> failwith (sprintf "Stack underflow at '%s' word" words.Head)
            if op <> Nop then op else failwith (sprintf "Invalid stack at '%s' word (stack: %A)" words.Head stack)
        let unop ops = seq {
            yield un ops
            yield! compile (stack |> pop |> push stack.Head) words.Tail }
        let binop ops = seq {
            yield bin ops
            yield! compile (stack |> pop |> pop |> push stack.Head) words.Tail }
        let uncomp ops = seq {
            yield un ops
            yield! compile (stack |> pop |> push I32) words.Tail }
        let bincomp ops = seq {
            yield bin ops
            yield! compile (stack |> pop |> pop |> push I32) words.Tail }
        let convert t ops = seq {
            yield un ops
            yield! compile (stack |> pop |> push t) words.Tail }
        let select = seq {
            yield Select
            let stack' =
                match stack with
                | t0 :: t1 :: _ :: stack' when t0 = t1 -> t0 :: stack'
                | _ -> failwith (sprintf "Invalid stack at '%s' word (stack: %A)" words.Head stack)
            yield! compile stack' words.Tail }
        let load t op = seq {
            match words with
            | _ :: offset :: align :: words' ->
                yield op (int offset, int align)
                yield! compile (t :: (pop stack)) words'
            | _ -> failwith (sprintf "Syntax error at '%s' word" words.Head) }
        let store op = seq {
            match words with
            | _ :: offset :: align :: words' ->
                yield op (int offset, int align)
                yield! compile (pop stack) words'
            | _ -> failwith (sprintf "Syntax error at '%s' word" words.Head) }
        match words with
        | "+" :: _ -> yield! binop (AddI32, AddI64, AddF32, AddF64)
        | "-" :: _ -> yield! binop (SubI32, SubI64, SubF32, SubF64)
        | "*" :: _ -> yield! binop (MulI32, MulI64, MulF32, MulF64)
        | "/" :: _ -> yield! binop (DivI32, DivI64, DivF32, DivF64)
        | "u/" :: _ -> yield! binop (DivUnsignedI32, DivUnsignedI64, Nop, Nop)
        | "rem" :: _ -> yield! binop (RemI32, RemI64, Nop, Nop)
        | "urem" :: _ -> yield! binop (RemUnsignedI32, RemUnsignedI64, Nop, Nop)
        | "and" :: _ -> yield! binop (AndI32, AndI64, Nop, Nop)
        | "or" :: _ -> yield! binop (OrI32, OrI64, Nop, Nop)
        | "xor" :: _ -> yield! binop (XorI32, XorI64, Nop, Nop)
        | "shl" :: _ -> yield! binop (ShiftLeftI32, ShiftLeftI64, Nop, Nop)
        | "shr" :: _ -> yield! binop (ShiftRightI32, ShiftRightI64, Nop, Nop)
        | "ushr" :: _ -> yield! binop (ShiftRightUnsignedI32, ShiftRightUnsignedI64, Nop, Nop)
        | "rotl" :: _ -> yield! binop (RotateLeftI32, RotateLeftI64, Nop, Nop)
        | "rotr" :: _ -> yield! binop (RotateRightI32, RotateRightI64, Nop, Nop)
        | "min" :: _ -> yield! binop (Nop, Nop, MinF32, MinF64)
        | "max" :: _ -> yield! binop (Nop, Nop, MaxF32, MaxF64)
        | "copysign" :: _ -> yield! binop (Nop, Nop, CopySignF32, CopySignF64)
        | "abs" :: _ -> yield! unop (Nop, Nop, AbsF32, AbsF64)
        | "neg" :: _ -> yield! unop (Nop, Nop, NegateF32, NegateF64)
        | "sqrt" :: _ -> yield! unop (Nop, Nop, SqrtF32, SqrtF64)
        | "ceil" :: _ -> yield! unop (Nop, Nop, CeilingF32, CeilingF64)
        | "floor" :: _ -> yield! unop (Nop, Nop, FloorF32, FloorF64)
        | "trunc" :: _ -> yield! unop (Nop, Nop, TruncateF32, TruncateF64)
        | "nearest" :: _ -> yield! unop (Nop, Nop, NearestF32, NearestF64)
        | "clz" :: _ -> yield! unop (ClzI32, ClzI64, Nop, Nop)
        | "ctz" :: _ -> yield! unop (CtzI32, CtzI64, Nop, Nop)
        | "popcnt" :: _ -> yield! unop (PopCountI32, PopCountI64, Nop, Nop)
        | "0=" :: _ -> yield! uncomp (EqualZeroI32, EqualZeroI64, Nop, Nop)
        | "=" :: _ -> yield! bincomp (EqualI32, EqualI64, EqualF32, EqualF64)
        | "<>" :: _ -> yield! bincomp (NotEqualI32, NotEqualI64, NotEqualF32, NotEqualF64)
        | "<" :: _ -> yield! bincomp (LessI32, LessI64, LessF32, LessF64)
        | "u<" :: _ -> yield! bincomp (LessUnsignedI32, LessUnsignedI64, Nop, Nop)
        | ">" :: _ -> yield! bincomp (GreaterI32, GreaterI64, GreaterF32, GreaterF64)
        | "u>" :: _ -> yield! bincomp (GreaterUnsignedI32, GreaterUnsignedI64, Nop, Nop)
        | "<=" :: _ -> yield! bincomp (LessOrEqualI32, LessOrEqualI64, LessOrEqualF32, LessOrEqualF64)
        | "u<=" :: _ -> yield! bincomp (LessOrEqualUnsignedI32, LessOrEqualUnsignedI64, Nop, Nop)
        | ">=" :: _ -> yield! bincomp (GreaterOrEqualI32, GreaterOrEqualI64, GreaterOrEqualF32, GreaterOrEqualF64)
        | "u>=" :: _ -> yield! bincomp (GreaterOrEqualUnsignedI32, GreaterOrEqualUnsignedI64, Nop, Nop)
        | ">i32" :: _ -> yield! convert I32 (Nop, WrapI64asI32, TruncateF32asI32, TruncateF64asI32)
        | "u>i32" :: _ -> yield! convert I32 (Nop, Nop, TruncateUnsignedF32asI32, TruncateUnsignedF64asI32)
        | ">i64" :: _ -> yield! convert I64 (ExtendI32toI64, Nop, TruncateF32asI64, TruncateF64asI64)
        | "u>i64" :: _ -> yield! convert I64 (ExtendUnsignedI32toI64, Nop, TruncateUnsignedF32asI64, TruncateUnsignedF64asI64)
        | ">f32" :: _ -> yield! convert F32 (ConvertI32toF32, ConvertI64toF32, Nop, DemoteF64toF32)
        | "u>f32" :: _ -> yield! convert F32 (ConvertUnsignedI32toF32, ConvertUnsignedI64toF32, Nop, Nop)
        | ">f64" :: _ -> yield! convert F64 (ConvertI32toF64, ConvertI64toF64, PromoteF32toF64, Nop)
        | "u>f64" :: _ -> yield! convert F64 (ConvertUnsignedI32toF64, ConvertUnsignedI64toF64, Nop, Nop)
        | ">>i32" :: _ -> yield! convert I32 (Nop, Nop, ReinterpretF32asI32, Nop)
        | ">>i64" :: _ -> yield! convert I64 (Nop, Nop, Nop, ReinterpretF64asI64)
        | ">>f32" :: _ -> yield! convert I64 (ReinterpretI32asF32, Nop, Nop, Nop)
        | ">>f64" :: _ -> yield! convert I64 (Nop, ReinterpretI64asF64, Nop, Nop)
        | "drop" :: words -> yield Drop; yield! compile (pop stack) words
        | "select" :: _ -> yield! select
        | "@>i32" :: _ -> yield! load I32 LoadI32
        | "@>i64" :: _ -> yield! load I64 LoadI64
        | "@>f32" :: _ -> yield! load F32 LoadF32
        | "@>f64" :: _ -> yield! load F64 LoadF64
        | "8@>i32" :: _ -> yield! load I32 LoadByteI32
        | "u8@>i32" :: _ -> yield! load I32 LoadByteUnsignedI32
        | "8@>i64" :: _ -> yield! load I64 LoadByteI64
        | "u8@>i64" :: _ -> yield! load I64 LoadByteUnsignedI64
        | "16@>i32" :: _ -> yield! load I32 LoadShortI32
        | "u16@>i32" :: _ -> yield! load I32 LoadShortUnsignedI32
        | "16@>i64" :: _ -> yield! load I64 LoadShortI64
        | "u16@>i64" :: _ -> yield! load I64 LoadShortUnsignedI64
        | "32@>i64" :: _ -> yield! load I64 LoadIntI64
        | "u32@>i64" :: _ -> yield! load I64 LoadIntUnsignedI64
        | "!i32" :: _ -> yield! store StoreI32
        | "!i64" :: _ -> yield! store StoreI64
        | "!f32" :: _ -> yield! store StoreF32
        | "!f64" :: _ -> yield! store StoreF64
        | "8!i32" :: _ -> yield! store StoreByteI32
        | "8!i64" :: _ -> yield! store StoreByteI64
        | "16!i32" :: _ -> yield! store StoreShortI32
        | "16!i64" :: _ -> yield! store StoreShortI64
        | "32!i64" :: _ -> yield! store StoreIntI64
        | "memsize" :: words -> yield CurrentMemory; yield! compile (I32 :: stack) words
        | "memgrow" :: words -> yield GrowMemory; yield! compile (I32 :: (pop stack)) words
        | num :: words -> // literal constants
            let parse n =
                let pre (n : string) = n.Substring(0, n.Length - 1)
                try
                    if num.Contains(".") then
                        if num.EndsWith('f') then num |> pre |> single |> ConstF32, F32 // 3.14f 2.71f 42.f
                        else num |> float |> ConstF64, F64 // 3.14 2.71 42.
                    else
                        if num.EndsWith('L') then num |> pre |> int64 |> ConstI64, I64 // 123L 10000000000L
                        else num |> int32 |> ConstI32, I32 // 123 (10000000000 fails)
                with _ -> failwith (sprintf "Unknown word '%s'" n)
            let n, t = parse num
            yield n; yield! compile (stack |> push t) words
        | [] ->
            if stack.Length > 1 then failwith "Multiple return values unsupported"
            yield End
    }
    source.Split(' ') |> Array.toList |> compile []

let print code =
    code |> List.ofSeq |> printfn "CODE: %A"
    code
