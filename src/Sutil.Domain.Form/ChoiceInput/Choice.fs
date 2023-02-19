namespace Sutil.Domain.Form
open Sutil
open Sutil.Core
open Sutil.CoreElements

open FsToolkit.ErrorHandling
open System


type ChoiceValue<'T> = 
    | Value of 'T
    | FormInput of FormElement<'T>
    with 
    member inline this.getCurrentValue() =
        match this with
        | Value value -> Validation.ok value
        | FormInput((formValue, _)) -> Store.current formValue


    member inline this.render(isSelected, dispatch) =
        match this with
        | FormInput (value', view) ->
            Html.div
                [ disposeOnUnmount [ Store.subscribe dispatch value' ]
                  Bind.toggleClass(isSelected, "show")
                  view 
                ]
        | _ -> Html.none

type Choice<'T> = {
    Selected : bool
    Label : SutilElement
    Value : ChoiceValue<'T>
}

module Choice = 
    let create ( v : 'T) = 
        { Value = Value(v) 
          Label = sprintf "%A" v |> text
          Selected = false
        }
    
    let createF( v : FormElement<'T> ) = 
        { Value = FormInput(v) 
          Label = sprintf "%A" v |> text
          Selected = false
        }
    
    let getCurrentValue model =  
        model.Value.getCurrentValue()

    let isSelected selected model = 
        { model with Selected = selected }

    let withLabel label model : Choice<'T>  = { model with Label = label}


(*
type Choice<'T> with
        member inline this.getCurrentValue() =
            Choice.getCurrentValue this
        
        member inline this.isSelected( selected ) = 
            Choice.isSelected selected this
        
        member inline this.isSelected() = 
            Choice.isSelected true this

        member inline this.withLabel( label ) = 
            Choice.withLabel label this

        member inline this.withLabel( label ) = 
            Choice.withLabel (text label) this
*)

type RuntimeChoice<'T> = {
    Selected : bool
    Value: 'T
}

type Choice =
    static member create( v : 'T) = 
        { Value = Value(v) 
          Label = sprintf "%A" v |> text
          Selected = false
        }
    
    static member createF( v : FormElement<'T> ) = 
        { Value = FormInput(v) 
          Label = sprintf "%A" v |> text
          Selected = false
        }

module ToggleComponent =

    type ToggleComponent =
    | CheckBox of string * IObservable<bool> * (unit -> unit)
    | RadioButton of string * IObservable<bool> * (unit -> unit)
    | ToggleButton of string * IObservable<bool> * (unit -> unit)

    let renderToggleComponent =
        function
        | CheckBox(name, isChecked, dispatch) -> 
            Html.div[
                Attr.name name
                Attr.role "checkbox"
                Attr.className ["checkbox"]
                Bind.toggleClass(isChecked, "checked")
                onClick (fun _ -> dispatch()) [] ]

        | RadioButton(name, isSelected, dispatch) ->
            Html.div [
                Attr.name name
                Attr.role "radio-button"
                Attr.className ["radio-button"]
                Bind.toggleClass(isSelected, "selected")
                onClick (fun _ -> dispatch()) [] ]

        | ToggleButton(name, isToggled, dispatch) ->
            Html.div [
                Attr.name name
                Attr.role "toggle-button"
                Attr.className ["toggle-button"]
                Bind.toggleClass(isToggled, "toggled")
                onClick (fun _ -> dispatch()) [] ]
