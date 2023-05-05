namespace Sutil.Domain.Form

open Sutil
open Sutil.CoreElements
open Sutil.Domain.Form
open Sutil.Core

open FsToolkit.ErrorHandling
open System

module Choice =

    /// <summary>
    /// The <c>ChoiceValue</c> deffines either a static value or another form.
    ///
    /// This allow composing forms together
    /// </summary>
    type ChoiceValue<'T> =
        | Value of 'T
        | FormInput of FormElement<'T>

        /// <summary>
        /// Gets the current value
        /// </summary>
        member inline this.getCurrentValue() =
            match this with
            | Value value -> Validation.ok value
            | FormInput((formValue, _)) -> Store.current formValue

        /// <summary>
        /// If the choice is a form, the form will be rendered and mount the value to the pipe og data. Othervise, nothing will happen
        /// </summary>
        member inline this.render(isSelected, dispatch) =
            match this with
            | FormInput(value', view) ->
                Html.div
                    [ disposeOnUnmount [ Store.subscribe dispatch value' ]
                      Bind.toggleClass (isSelected, "show")
                      view ]
            | _ -> Html.none

    /// <summary>
    /// Contains a <a href="Choice.ChoiceValue">ChoiceValue</a>, Label and selected state
    /// </summary>
    type Choice<'T> =
        { Selected: bool
          Label: SutilElement
          Value: ChoiceValue<'T> }

    /// <summary>
    /// Create a static choice type
    /// </summary>
    /// <example>
    ///   <code>
    ///     let fsharpOption = Choice.create "F#"
    ///     let csharpOption = Choice.create "C#"
    ///   </code>
    /// </example>
    let create (v: 'T) =
        { Value = Value(v)
          Label = sprintf "%A" v |> text
          Selected = false }

    let createF (v: FormElement<'T>) label =
        { Value = FormInput(v)
          Label = label
          Selected = false }

    let getCurrentValue model = model.Value.getCurrentValue ()

    let isSelected selected model = { model with Selected = selected }

    let withLabel label model : Choice<'T> = { model with Label = label }


    type RuntimeChoice<'T> = { Selected: bool; Value: 'T }

    type Choice =
        static member create(v: 'T) =
            { Value = Value(v)
              Label = sprintf "%A" v |> text
              Selected = false }

        static member create(v: 'T, label) =
            { Value = Value(v)
              Label = label
              Selected = false }

        static member create(v: FormElement<'T>, label) =
            { Value = FormInput(v)
              Label = label
              Selected = false }

module ToggleComponent =

    type ToggleComponent =
        | CheckBox of string * IObservable<bool> * (unit -> unit)
        | RadioButton of string * IObservable<bool> * (unit -> unit)
        | ToggleButton of string * IObservable<bool> * (unit -> unit)

    let renderToggleComponent =
        function
        | CheckBox(name, isChecked, dispatch) ->
            Html.div[Attr.name name
                     Attr.role "checkbox"
                     Attr.className [ "checkbox" ]
                     Bind.toggleClass (isChecked, "checked")
                     onClick (fun _ -> dispatch ()) []]

        | RadioButton(name, isSelected, dispatch) ->
            Html.div
                [ Attr.name name
                  Attr.role "radio-button"
                  Attr.className [ "radio-button" ]
                  Bind.toggleClass (isSelected, "selected")
                  onClick (fun _ -> dispatch ()) [] ]

        | ToggleButton(name, isToggled, dispatch) ->
            Html.div
                [ Attr.name name
                  Attr.role "toggle-button"
                  Attr.className [ "toggle-button" ]
                  Bind.toggleClass (isToggled, "toggled")
                  onClick (fun _ -> dispatch ()) [] ]
