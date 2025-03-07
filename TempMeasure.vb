Namespace OWEN

    ''' <summary>
    ''' Измерение температуры и сопутствующих параметров по 8-ми каналам от прибора ОВЕН.
    ''' </summary>
    Public Class TempMeasure

#Region "CTOR"

        Public Sub New(ts As Date, temps As IEnumerable(Of Double?))

            Timestamp = ts

            If (Not StartedSet) Then
                StartedSet = True
                _Started = ts
            End If
            If (Finished < ts) Then
                _Finished = ts
            End If

            _Temperature = New Double?(temps.Count - 1) {}
            For i As Integer = 0 To temps.Count - 1

                _Temperature(i) = temps(i)

                If _Temperature(i).HasValue Then

                    ActiveChannels(i) = True

                    If (_Temperature(i) > MaxValue) Then
                        _MaxValue = _Temperature(i).Value
                    End If
                    If (_Temperature(i) < MinValue) Then
                        _MinValue = _Temperature(i).Value
                    End If

                End If
            Next
        End Sub

        ''' <summary>
        ''' Разбирает измерения из строки журнала.
        ''' </summary>
        ''' <param name="line">Строка журнала. Формат строки: 20.02.2025 16:00:00	25.6	-	-	-	-	-	-	-</param>
        Public Sub New(line As String)
            Dim parts As String() = line.Split(CChar(vbTab))

            Timestamp = Date.ParseExact(parts(0), "dd.MM.yyyy HH:mm:ss", Nothing)

            If (Not StartedSet) Then
                StartedSet = True
                _Started = Timestamp
            End If
            If (Finished < Timestamp) Then
                _Finished = Timestamp
            End If

            _Temperature = New Double?(Ukt38.NUM_CHANNELS - 1) {}
            For i As Integer = 0 To Ukt38.NUM_CHANNELS - 1
                Dim t As Double
                If (Double.TryParse(parts(i + 1), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, t)) Then

                    _Temperature(i) = t
                    ActiveChannels(i) = True

                    If (t > MaxValue) Then
                        _MaxValue = t
                    End If
                    If (t < MinValue) Then
                        _MinValue = t
                    End If

                Else
                    _Temperature(i) = Nothing
                End If
            Next
        End Sub

#End Region '/CTOR

#Region "PROPS"

        Public ReadOnly Property Timestamp As Date

        ''' <summary>
        ''' Значения температуры по 8-ми каналам (в градусах Цельсия).
        ''' </summary>
        Public ReadOnly Property Temperature As Double?() = {Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing}

        Public Shared ReadOnly Property MaxValue As Double = Double.NegativeInfinity
        Public Shared ReadOnly Property MinValue As Double = Double.PositiveInfinity
        Private Shared StartedSet As Boolean = False
        Public Shared ReadOnly Property Started As New Date()
        Public Shared ReadOnly Property Finished As New Date()

        ''' <summary>
        ''' Если в канале были данные, то считаем его активным.
        ''' </summary>
        Public Shared ReadOnly Property ActiveChannels As Boolean() = {False, False, False, False, False, False, False, False}

        ''' <summary>
        ''' Число активных каналов.
        ''' </summary>
        Public Shared ReadOnly Property NumActiveChannels As Integer
            Get
                Return (From c In ActiveChannels Where c).Count()
            End Get
        End Property

#End Region '/PROPS

#Region "METHODS"

        ''' <summary>
        ''' Сбрасывает статические значения <see cref="MinValue"/>, <see cref="MaxValue"/>, 
        ''' значение активных каналов <see cref="ActiveChannels"/>, 
        ''' а также время начала <see cref="Started"/> и конца измерений <see cref="Finished"/>.
        ''' </summary>
        Public Shared Sub ResetStaticProps()
            _MaxValue = Double.NegativeInfinity
            _MinValue = Double.PositiveInfinity
            StartedSet = False
            _Started = New Date()
            _Finished = New Date()
            For i As Integer = 0 To ActiveChannels.Length - 1
                ActiveChannels(i) = False
            Next
        End Sub

        Public Overrides Function ToString() As String
            Dim sb As New Text.StringBuilder()
            sb.Append($"{Timestamp:dd.MM.yyyy HH:mm:ss}")
            sb.Append(vbTab)
            For Each c In Temperature
                sb.Append(If(c.HasValue, c.Value.ToString("F3", Globalization.CultureInfo.InvariantCulture), "-"))
                sb.Append(vbTab)
            Next
            Return sb.ToString()
        End Function

#End Region '/METHODS

    End Class

End Namespace
