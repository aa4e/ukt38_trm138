Namespace Owen

    ''' <summary>
    ''' Класс содержит информацию, связанную с любыми возникающими ошибками.
    ''' </summary>
    Public Class ErrorData

#Region "CTORs"

        Friend Sub New()
        End Sub

        ''' <param name="text">Описание ошибки.</param>
        Friend Sub New(text As String)
            ErrorText = text
            HasError = True
        End Sub

        ''' <param name="exception">Перехваченное исключение.</param>
        Friend Sub New(exception As Exception)
            ErrorText = exception.Message
            Me.Exception = exception
            HasError = True
        End Sub

#End Region '/CTORs

#Region "PROPS"

        ''' <summary>
        ''' Была ли ошибка.
        ''' </summary>
        Public ReadOnly Property HasError As Boolean

        ''' <summary>
        ''' Текстовое описание ошибки.
        ''' </summary>
        Public ReadOnly Property ErrorText As String

        ''' <summary>
        ''' Перехваченное исключение (если было).
        ''' </summary>
        Public ReadOnly Property Exception As Exception

#End Region '/PROPS

    End Class

End Namespace
