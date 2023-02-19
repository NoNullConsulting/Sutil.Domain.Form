namespace Sutil.Domain.Form

open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result
open Sutil
open System
open Sutil.Core

type SingleChoiceInput<'TValue, 'TResult when 'TValue : equality> = 
    { Values: Choice<'TValue> list
      IsChecked: bool
      IsRequired: bool
      Parser: 'TValue option -> Validation<'TResult, string>
      ContainerProps: FormElementContainer
      ToggleComponent: (string * IObservable<bool> * (unit -> unit)) -> SutilElement
    } 
    interface IFormElement<SingleChoiceInput<'TValue, 'TResult>> with
        member controls.UpdateFormContainer(mapper) =
            { controls with ContainerProps = mapper controls.ContainerProps }


type SingleChoiceInputViewModel<'T, 'R> = 
    { Choices : RuntimeChoice<Validation<'T,string>> list }
    with 
    member this.IsChecked index = 
        this.Choices 
        |> List.item index
        |> fun c -> c.Selected

    member this.Result parser = 
        this.Choices
        |> List.tryFind(fun item -> item.Selected)
        |> Option.map(fun item -> item.Value)
        |> Option.sequenceResult
        |> Validation.bind parser


type SingleChoiceInputMsg<'T> =
    | Toggle of int
    | SetValue of int * Validation<'T,string>


module SingleChoiceViewModel =
    let init (values : Choice<'T> list) = 
        { Choices =
            values 
            |> List.map (fun (v: Choice<'T>) -> 
                { Selected = v.Selected; 
                  Value = v.Value.getCurrentValue() 
                })
        }

    let update msg model =
        match msg with
        | Toggle index ->
            let nextChoices = List.mapi (fun i v -> { Selected = index = i && not v.Selected; Value = v.Value }) model.Choices 
            { model with Choices = nextChoices }

        | SetValue(index, value) ->             
            let nextChoices = model.Choices.setI(index, (fun v -> { v with Value = value }))
            { model with Choices = nextChoices }
 

open SingleChoiceViewModel
module SingleChoiceInput =
    let create values =
        { Values = values
          Parser = Ok
          IsChecked = false
          IsRequired = false
          ContainerProps = FormElementContainer.create ()
          ToggleComponent = ToggleComponent.RadioButton >> ToggleComponent.renderToggleComponent
        }

    
    let inline withOptionParser parser controls =
        { Values = controls.Values
          Parser = parser
          IsChecked = controls.IsChecked
          ContainerProps = controls.ContainerProps
          IsRequired = controls.IsRequired
          ToggleComponent = controls.ToggleComponent 
        }

    let inline withValueParser parser controls =
        withOptionParser (fun x -> Option.isRequired x >>= parser) controls


    let inline isRequired (controls: SingleChoiceInput<_,_>) =
        { controls with IsRequired = true }


    let inline render controls =
        let viewModel, dispatch = Store.makeElmishSimple init update ignore controls.Values
        let result = viewModel.map( fun m -> m.Result controls.Parser )

        let createChoiceElement index (choice : Choice<'TValue>) =
            let isSelected = viewModel ..> (fun x -> x.IsChecked index)
            
            Html.div 
                [ Html.label [ choice.Label ]
                  controls.ToggleComponent("", isSelected, fun () -> Toggle index |> dispatch)
                  choice.Value.render(isSelected, fun v -> SetValue(index, v) |> dispatch) 
                ]

        result, controls.ContainerProps.render
            [ Bind.toggleClass(isError' result, "error")
              yield! List.mapi createChoiceElement controls.Values
              Html.div 
                [ Bind.each (getErrors' result, (fun err -> Html.span [ Html.text err ])) 
                ]
            ]


type SingleChoiceInput<'TValue, 'TResult when 'TValue : equality> with
    member inline controls.withParser( parser ) =
        SingleChoiceInput.withValueParser parser controls

    member inline controls.withParser( parser ) =
        SingleChoiceInput.withOptionParser parser controls

    member inline controls.isRequired() = 
        SingleChoiceInput.isRequired controls

    member inline controls.render() = 
        SingleChoiceInput.render controls