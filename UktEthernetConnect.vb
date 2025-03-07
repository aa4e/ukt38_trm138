Imports System.Net.Sockets

Namespace OWEN

    Public Class UktEthernetConnect
        Implements IConnect

#Region "PROPS, FIELDS"

        Private Client As TcpClient
        Private Hostname As String
        Private Port As Integer

        Public ReadOnly Property IsOpen As Boolean Implements IConnect.IsOpen
            Get
                Return If(Client IsNot Nothing, Client.Connected, False)
            End Get
        End Property

#End Region '/PROPS, FIELDS

#Region "METHODS"

        Public Sub SetParamaters(ParamArray params As Object()) Implements IConnect.SetParamaters
            Me.Hostname = params(0).ToString()
            Me.Port = CInt(params(1))
        End Sub

        Public Function Open() As Boolean Implements IConnect.Open
            If (Client Is Nothing) Then
                Client = New TcpClient()
            End If

            Client.Connect(Hostname, Port)

            'читаем весь мусор из буфера:
            Do While (Client.Available > 0)
                Dim thrash As Integer = Client.GetStream().ReadByte()
            Loop

            InitMeasurements()

            Return True
        End Function

        Public Function Close() As Boolean Implements IConnect.Close
            Try
                If (Client IsNot Nothing) Then
                    Client.Close()
                    Client = Nothing
                End If
            Catch ex As Exception
                Debug.WriteLine(ex)
                Return False
            End Try
            Return True
        End Function

        ''' <summary>
        ''' Инициирует измерения.
        ''' </summary>
        Private Sub InitMeasurements()

            Dim initMeasure As Byte() = {Ukt38.Mark.StartSession, Ukt38.ReadLen.READ_16_BYTE, Ukt38.Registers.Temp}
            Client.GetStream().Write(initMeasure, 0, initMeasure.Length)

            'ждём данных
            Dim sw As New Stopwatch()
            sw.Start()
            Do While (sw.Elapsed.TotalMilliseconds() < 2000) AndAlso (Client.Available < 21)
                Threading.Thread.Sleep(10)
            Loop
            sw.Stop()

            'Считываем первый ответ прибора:
            Dim ans As New List(Of Byte)
            Do While (Client.Available > 0)
                Dim b As Integer = Client.GetStream().ReadByte()
                If (b >= 0) Then
                    ans.Add(CByte(b))
                End If
            Loop

        End Sub

        Public Function ReadTemperature() As Double?() Implements IConnect.ReadTemperature

            'Ждём данные:
            Const PAK_LEN As Integer = 21 '21 = [start + state + len + reg + 16 байт данных + crc]
            Do While IsOpen AndAlso (Client.Available < PAK_LEN)
                Threading.Thread.Sleep(10)
            Loop

            'Читаем всё пришедшее:
            Dim data As New List(Of Byte)
            If IsOpen Then
                Do While (data.Count <= 100) AndAlso (Client.Available > 0)
                    Dim b As Integer = Client.GetStream().ReadByte()
                    If (b >= 0) Then
                        data.Add(CByte(b))
                    End If
                Loop
            End If

            Dim matchesFound As Boolean
            For Each b In data
                If (b = Ukt38.Mark.DeviceReady) Then
                    matchesFound = True
                    Exit For
                End If
            Next
            If (Not matchesFound) Then
                Debug.WriteLine($"{Now} Прибор не готов")
                'Throw New Exception($"{Now} Прибор не готов")
            End If

            Dim temperature As Double?() = Ukt38.ParseTemperature(data)
            Return temperature

        End Function

#End Region '/METHODS

    End Class

End Namespace
