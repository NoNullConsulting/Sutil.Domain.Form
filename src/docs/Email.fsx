(**
---
category: Documentation
categoryindex: 0
index: 1
---
*)
(**

Create simple (and stupid) `Email` validation

*)
(***hide***)
#r "/Users/allankjaer/Documents/NoNullConsulting/Sutil.Domain.Form/src/Sutil.Domain.Form/bin/Release/net7.0/Sutil.Domain.Form.dll"
#r "nuget: FsToolkit.ErrorHandling"
#r "nuget: Sutil"
(** *)

open Sutil.Domain.Form

module Email =
    open FsToolkit.ErrorHandling

    type Email = private Email of string

    let create (str : string) =
        if str.Contains("@") then
            Email str |> Ok
        else
            Validation.error "Not a valid Email"

let emailField =  // FormElement<Email>
    TextInput.create()
        .setLabel( "Email" )
        .setPlaceholder( "Email" )
        .withParser( Email.create )
        .render()
