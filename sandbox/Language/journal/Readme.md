# Journal

* [December 2020](https://github.com/AshleyF/brief/blob/gh-pages/sandbox/Language/journal/DEC2020.md)
* [January 2021](https://github.com/AshleyF/brief/blob/gh-pages/sandbox/Language/journal/JAN2021.md)

## Ideas

- Strip the interpreter down to a bare minimum (no debugger, no `_return` handling) and then rewrite the lexer/parser/compiler in Brief itself. Compile to what though? Perhaps change the interpreter to more of a "VM" with a binary format requiring no parsing.
- Move `_return` from the interpreted back to a primitive word and change `let` to append it rather than the interpreter.
- Precompile secondaries with scopes (environment) attached.

## Notes

- Prefix vs. postfix notation:
	- Names like `prepose`/`compose` reversed
	- "Natural" order of `-`, `/`, etc. reversed
	- Reversed `let` expressions seem weird, but "red" words might fix that
