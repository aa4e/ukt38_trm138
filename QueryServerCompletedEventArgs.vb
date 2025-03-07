Namespace Owen

    ''' <summary>
    ''' Аргументы, передаваемые в событии <see cref="Ukt38.QueryServerCompleted"/>.
    ''' </summary>
    Public Class QueryServerCompletedEventArgs
        Inherits EventArgs

        Public Property Data As TempMeasure
        Public Property ErrorData As New ErrorData()
        Public Property Succeeded As Boolean

    End Class

End Namespace
