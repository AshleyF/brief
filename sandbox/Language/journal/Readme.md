# Journal

* [December 2020](https://github.com/AshleyF/brief/blob/gh-pages/sandbox/Language/journal/DEC2020.md)
* [January 2021](https://github.com/AshleyF/brief/blob/gh-pages/sandbox/Language/journal/JAN2021.md)
* [October 2021](https://github.com/AshleyF/brief/blob/gh-pages/sandbox/Language/journal/OCT2021.md)
* [November 2021](https://github.com/AshleyF/brief/blob/gh-pages/sandbox/Language/journal/NOV2021.md)

## Ideas

- Strip the interpreter down to a bare minimum (no debugger, no `_return` handling) and then rewrite the lexer/parser/compiler in Brief itself. Compile to what though? Perhaps change the interpreter to more of a "VM" with a binary format requiring no parsing.
- Move `_return` from the interpreted back to a primitive word and change `let` to append it rather than the interpreter.
- Precompile secondaries with scopes (environment) attached.
- Instead of structure-serialization, a "bytecode" allowing structure building

## Notes

- Prefix vs. postfix notation:
	- Names like `prepose`/`compose` reversed
	- "Natural" order of `-`, `/`, etc. reversed
	- Reversed `let` expressions seem weird, but "red" words might fix that
    - Continuation is in reverse internally and translated to a quotation to match prefix
    - It feels weird that `foo bar [ENTER]` is different to `foo [ENTER]` then `bar [ENTER]` at the REPL
        - Tend to build piecewise at the REPL, but then need to assemble in _reverse_ into a definition
    - Similar to F#'s |> or >> operator -- feels natural to compose in this direction

## Secondaries with in-built defintions

One idea is to lazily populate definitions directly in secondary words without dictionary lookup:

```fsharp
and Word(name: string) =
    member _.Name = name
    override this.Equals(o) =
        match o with
            | :? Word as w -> this.Name = w.Name
            | _ -> false
    override this.GetHashCode() = hash (this.Name)
    interface IComparable with
        override this.CompareTo(o) =
            match o with
                | :? Word as w -> compare this.Name w.Name
                | _ -> -1

and PrimitiveWord(name: string, func: State -> State) =
    inherit Word(name)
    member _.Func = func

and SecondaryWord(name: string) =
    inherit Word(name) // assumes name is unique (e.g. namespace prepended)
    member val Definition : Value option = None with get, set
```
