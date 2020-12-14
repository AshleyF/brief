module Prelude

open Structure
open Primitives
open Syntax

let define name source dictionary = Map.add name (Secondary (name, brief dictionary source)) dictionary

let prelude =
    primitives
    |> define "pi"   "3.14159"
    |> define "e"    "2.71828"
    |> define "sq"   "dup *"
    |> define "area" "sq pi *"
