Imports System.IO.Ports

Namespace OWEN

    Public Class UktUartConnect
        Implements IConnect

#Region "PROPS, FIELDS"

        Private Port As SerialPort
        Private Name As String

        Public ReadOnly Property IsOpen As Boolean Implements IConnect.IsOpen
            Get
                Return If(Port IsNot Nothing, Port.IsOpen, False)
            End Get
        End Property

#End Region '/PROPS, FIELDS

#Region "METHODS"

        Public Sub SetParamaters(ParamArray params As Object()) Implements IConnect.SetParamaters
            Dim name As String = params(0).ToString()
            Me.Name = name
        End Sub

        Public Function Open() As Boolean Implements IConnect.Open
            Port = New SerialPort(Name, 9600, Parity.Even, 8, StopBits.Two) With {
                .ReadTimeout = 1000,
                .WriteTimeout = 1000
            }
            Port.Open()
            Port.DiscardInBuffer()
            Port.DiscardOutBuffer()
            Return True
        End Function

        Public Function Close() As Boolean Implements IConnect.Close
            Try
                If (Port IsNot Nothing) Then
                    Port.Close()
                    Port = Nothing
                End If
            Catch ex As Exception
                Debug.WriteLine(ex)
                Return False
            End Try
            Return True
        End Function

        Public Function ReadTemperature() As Double?() Implements IConnect.ReadTemperature

            Port.Write({Ukt38.Mark.StartSession}, 0, 1) 'начало обмена

            'Проверка готовности прибора:
            Dim data As New List(Of Byte)
            Do While (data.Count < 2)
                Dim b As Integer = Port.ReadByte()
                If (b >= 0) Then
                    data.Add(CByte(b))
                Else
                    Exit Do
                End If
            Loop

            'Передача и чтение полезных данных:
            Dim ask As Byte() = {Ukt38.ReadLen.READ_16_BYTE, Ukt38.Registers.Temp}
            Port.Write(ask, 0, ask.Length)

            Do While (data.Count <= Ukt38.LengthInBytes(Ukt38.ReadLen.READ_16_BYTE) + 4)
                Dim b As Integer = Port.ReadByte()
                If (b >= 0) Then
                    data.Add(CByte(b))
                End If
            Loop

            Dim temperature As Double?() = Ukt38.ParseTemperature(data)
            Return temperature

        End Function

#End Region '/METHODS

    End Class

End Namespace
