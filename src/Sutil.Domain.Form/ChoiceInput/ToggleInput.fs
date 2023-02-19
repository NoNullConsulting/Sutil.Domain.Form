namespace Sutil.Domain.Form

open Sutil
open Sutil.Core

open Sutil.Domain.Form
open FsToolkit.ErrorHandling
open System

type CheckboxInput<'TValue, 'TResult> = 
    { Choice : Choice<'TValue>
      IsChecked : bool
      Parser: 'TValue option -> Validation<'TResult, string>
      ContainerProps : FormElementContainer
      ToggleComponent: (string * IObservable<bool> * (unit -> unit)) -> SutilElement
    }
    interface IFormElement<CheckboxInput<'TValue, 'TResult>> with
        member model.UpdateFormContainer( mapper ) =
            { model with ContainerProps = mapper model.ContainerProps }


type CheckboxInputViewModel<'T> = { 
    IsSelected : bool
    Value: Validation<'T,string>
} 


type CheckboxInputMsg<'T> = 
    | Toggle
    | SetValue of Validation<'T,string>


module CheckboxInputViewModel =
    let computeValue parser isSelected value =
        if isSelected
        then value |> Result.map Some |> Validation.bind parser
        else parser None

    let init controls =
        let ({ Parser = parser; IsChecked = isChecked; Choice = choice }) = controls

        { 
            IsSelected = isChecked
            Value = computeValue parser isChecked (choice.Value.getCurrentValue())
        }

    let update controls =
        let validator = computeValue controls.Parser

        fun msg model ->
            match msg with
            | Toggle -> 
                { model with IsSelected = not model.IsSelected }

            | SetValue v -> 
                { model with Value = validator model.IsSelected v } 


open CheckboxInputViewModel;
module CheckboxInput =
    let create value = { 
        Choice = value
        Parser = Ok
        IsChecked = false
        ContainerProps = FormElementContainer.create ()
        ToggleComponent = ToggleComponent.ToggleButton >> ToggleComponent.renderToggleComponent
    }
    
    let withOptionParser parser model = { 
        Choice = model.Choice
        Parser = parser
        IsChecked = model.IsChecked
        ContainerProps = model.ContainerProps
        ToggleComponent = model.ToggleComponent
    }

    let withValueParser' parser model = 
        let parser' = function 
            | Some x -> parser x
            | _ -> Validation.error "This is required"
        withOptionParser parser' model

    let render controls : FormElement<_> =
        let vm', dispatch = Store.makeElmishSimple init (update controls) ignore controls
        let isSelected = vm'.map(fun vm -> vm.IsSelected)
        let result = vm'.map( fun vm -> vm.Value)                        

        result, controls.ContainerProps.render [ 
            Bind.toggleClass(isError' result, "error" )
            
            controls.ToggleComponent("", isSelected, fun () -> dispatch Toggle)
            controls.Choice.Value.render(isSelected, fun value -> SetValue value |> dispatch)
            
            Html.div [ 
                Bind.each(getErrors' result, fun err -> Html.span [ Html.text err ] ) 
            ]
        ]

type CheckboxInput<'TValue, 'TResult> with
    member inline model.withParser( parser ) = 
        CheckboxInput.withOptionParser parser model

    member inline model.withParser( parser ) =
        CheckboxInput.withValueParser' parser model

    member inline model.render() =
        CheckboxInput.render model