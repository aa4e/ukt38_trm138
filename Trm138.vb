Namespace OWEN

    ''' <summary>
    ''' ТРМ138 измеритель температуры 8-канальный.
    ''' </summary>
    Public Class Trm138
        Inherits DeviceBase

#Region "CTOR"

        Public Sub New()
            MyBase.New()
            Connection = Connection.Usb
        End Sub

#End Region '/CTOR

#Region "PROPS"

        Public Overrides ReadOnly Property DeviceType As DeviceType = OWEN.DeviceType.Trm138

        Public Overrides Property Connection As Connection
            Get
                Return _Connection
            End Get
            Set(value As Connection)
                If (_Connection <> value) Then
                    _Connection = value
                    NotifyPropertyChanged(NameOf(Connection))
                    Client?.Close()
                    Select Case value
                        Case Connection.Usb
                            Client = New TrmUsbConnect(Me)
                            Client.SetParamaters(PortName)
                    End Select
                End If
            End Set
        End Property
        Private _Connection As Connection = Connection.None

        Public Overrides ReadOnly Property ConnectionsDict As New Dictionary(Of Connection, String) From {
            {Connection.Usb, "Usb"}
        }

#End Region '/PROPS

    End Class

End Namespace
