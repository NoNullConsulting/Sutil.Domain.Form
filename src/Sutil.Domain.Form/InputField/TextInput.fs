namespace Sutil.Domain.Form

open FsToolkit.ErrorHandling
open Sutil

type InputType =
    | Text
    | Password

type TextInput<'TResult when 'TResult : equality> = 
    { Placeholder : string
      Parser: string -> Validation<'TResult, string>
      InputType : InputType
      AutoCompleate : string option
      ContainerProps : FormElementContainer 
    }
    with 
        member inline model.setPlaceholder(placeholder) =
            { model with Placeholder = placeholder }

        member inline model.isPassword() =
            { model with InputType = Password }

        member inline model.withParser (parser) =
            { Placeholder = model.Placeholder
              InputType = model.InputType
              AutoCompleate = model.AutoCompleate
              ContainerProps = model.ContainerProps
              Parser = parser
            }

        member inline model.render () : FormElement<_> =
            let primitive = Store.make ""

            let result = primitive ..> model.Parser

            let view = model.ContainerProps.render [
                Bind.toggleClass(isError' result, "error" )
                Html.input [
                    match model.InputType with
                    | Text -> Attr.typeText
                    | Password -> Attr.typePassword
                    Bind.attr("value", Store.set primitive)
                ]
                Html.div [
                    Bind.each(getErrors' result, fun err -> Html.span [Html.text err] )
                ]
            ]

            result, view

        interface IFormElement<TextInput<'TResult>> with
            member model.UpdateFormContainer( mapper ) =
                {model with ContainerProps = mapper model.ContainerProps}

module TextInput = 
    let create() : TextInput<string> =
        { Placeholder = ""
          Parser = Ok
          InputType = Text
          AutoCompleate = None
          ContainerProps = FormElementContainer.create()
        }