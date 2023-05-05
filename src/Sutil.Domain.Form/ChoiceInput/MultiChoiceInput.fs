namespace Sutil.Domain.Form

open Sutil.Domain.Form.Choice
open FsToolkit.ErrorHandling
open Sutil.Core
open System

type MultiChoiceInput<'TValue, 'TResult when 'TValue: equality> =
    { Values: Choice<'TValue> list
      IsChecked: bool
      Parser: 'TValue option list -> Validation<'TResult, string>
      ContainerProps: FormElementContainer
      ToggleComponent: (string * IObservable<bool> * (unit -> unit)) -> SutilElement }

    interface IFormElement<MultiChoiceInput<'TValue, 'TResult>> with
        member model.UpdateFormContainer(mapper) =
            { model with
                ContainerProps = mapper model.ContainerProps }


type ViewModel<'T> =
    { Selected: bool
      Value: Validation<'T, string> }

type Msg<'T> =
    | Toggle of int
    | SetValue of int * 'T

module ViewModel =
    let init (values: Choice<_> list) =
        values
        |> List.map (fun v ->
            { Value = v.Value.getCurrentValue ()
              Selected = v.Selected })


    let update msg (model: ViewModel<'T> list) =
        match msg with
        | Toggle index -> model.setI (index, (fun v -> { v with Selected = not v.Selected }))

        | SetValue(i, value) -> model.setI (i, (fun v -> { v with Value = value }))


    let combineValidations = List.sequenceValidationA


    let getValueIfSelected model =
        if model.Selected then Some model.Value else None
        |> Option.sequenceResult


    let validateResult parser choices =
        choices
        |> List.map getValueIfSelected
        |> combineValidations
        |> Validation.bind parser


module MultiChoiceInput =
    open Sutil

    let create values =
        { Values = values
          Parser = Ok
          IsChecked = false
          ContainerProps = FormElementContainer.create ()
          ToggleComponent = ToggleComponent.CheckBox >> ToggleComponent.renderToggleComponent }


    let inline withListOptionParser parser controls =
        { Values = controls.Values
          Parser = parser
          IsChecked = controls.IsChecked
          ContainerProps = controls.ContainerProps
          ToggleComponent = controls.ToggleComponent }


    let inline withOptionParser parser controls =
        withListOptionParser (List.map parser >> ViewModel.combineValidations) controls


    let inline withListValueParser parser controls =
        withListOptionParser ((List.collect Option.toList) >> parser) controls


    let inline render controls =
        let viewModel, dispatch =
            Store.makeElmishSimple (ViewModel.init) ViewModel.update ignore controls.Values

        let getIsSelected index =
            viewModel.map (List.item index >> fun ({ Selected = s }) -> s) |> Store.distinct

        let result =
            viewModel.map (fun choices -> ViewModel.validateResult controls.Parser choices)

        let renderInput index choice =
            let isSelected = getIsSelected index

            Html.div
                [ Html.label [ choice.Label ]
                  controls.ToggleComponent("", isSelected, (fun () -> Toggle index |> dispatch))
                  choice.Value.render (isSelected, (fun v -> SetValue(index, v) |> dispatch)) ]

        result,
        controls.ContainerProps.render
            [ Bind.toggleClass (isError' result, "error")
              yield! List.mapi renderInput controls.Values
              Html.div [ Bind.each (getErrors' result, (fun err -> Html.span [ Html.text err ])) ] ]
