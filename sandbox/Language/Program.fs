open System
open Structure
open Syntax
open Print
open Prelude
open Interpretation

"7.2 area" |> brief prelude |> eval emptyState true |> printState
