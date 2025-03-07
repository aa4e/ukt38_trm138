Imports System.IO.Ports

Namespace OWEN

    ''' <summary>
    ''' Подключение через USB-UART мост.
    ''' </summary>
    Public Class TrmUsbConnect
        Implements IConnect

#Region "PROPS, FIELDS"

        Private Port As SerialPort
        Private Name As String

        Public ReadOnly Property IsOpen As Boolean Implements IConnect.IsOpen
            Get
                Return (Port IsNot Nothing) AndAlso Port.IsOpen
            End Get
        End Property

#End Region '/PROPS, FIELDS

#Region "CTOR"

        Private ReadOnly Device As Trm138

        Public Sub New(dev As DeviceBase)
            Me.Device = CType(dev, Trm138)
        End Sub

#End Region '/CTOR

#Region "METHODS"

        Public Sub SetParamaters(ParamArray params As Object()) Implements IConnect.SetParamaters
            Dim name As String = params(0).ToString()
            Me.Name = name
        End Sub

        Public Function ReadTemperature() As Double?() Implements IConnect.ReadTemperature

            Dim temperatures As New List(Of Double?)
            For i As Integer = 0 To Trm138.NUM_CHANNELS - 1

                Port.Write(GetTempRequestPacket(i).PacketString)

                Dim data As New List(Of Byte)
                Do While (data.Count <= 25)
                    Dim b As Integer = Port.ReadByte()
                    If (b = Asc(Packet.PAK_END)) Then
                        Exit Do
                    End If
                    If (b >= 0) Then
                        data.Add(CByte(b))
                    End If
                Loop

                Dim ansText As String = Text.Encoding.ASCII.GetString(data.ToArray())

                Dim ans As New Packet(ansText)
                Dim temp As Single? = Nothing
                If (ans.RawData.Hash = HashCode.Read) Then
                    temp = BitConverter.ToSingle(ans.RawData.Data.Take(4).Reverse().ToArray(), 0)
                End If

                temperatures.Add(temp)
            Next

            Return temperatures.ToArray()

        End Function

        Public Function Open() As Boolean Implements IConnect.Open
            Try
                Port = New SerialPort(Name, 115200, Parity.None, 8, StopBits.One) With {
                    .ReadTimeout = 1000,
                    .WriteTimeout = 1000
                }
                Port.Open()
            Catch ex As Exception
                Debug.WriteLine(ex)
                Return False
            End Try
            Return True
        End Function

        Public Function Close() As Boolean Implements IConnect.Close
            Try
                If (Port IsNot Nothing) Then
                    Port.Close()
                End If
            Catch ex As Exception
                Debug.WriteLine(ex)
                Return False
            End Try
            Return True
        End Function

        ''' <summary>
        ''' Возвращает пакет для запроса температуры.
        ''' </summary>
        Private Function GetTempRequestPacket(index As Integer) As Packet
            Return New Packet(Device.BaseAddress + index, HashCode.Read, True, 0, Nothing)
        End Function

#End Region '/METHODS

    End Class

End Namespace
