Namespace OWEN

    ''' <summary>
    ''' УКТ38 измеритель температуры 8-канальный.
    ''' </summary>
    Public Class Ukt38
        Inherits DeviceBase

#Region "CTOR"

        Public Sub New()
            MyBase.New()
            Connection = Connection.Eth
        End Sub

#End Region '/CTOR

#Region "PROPS"

        Public Overrides ReadOnly Property DeviceType As DeviceType = OWEN.DeviceType.Ukt38

        Public Property Hostname As String
            Get
                Return _Hostname
            End Get
            Set(value As String)
                If (_Hostname <> value) Then
                    _Hostname = value
                    NotifyPropertyChanged(NameOf(Hostname))
                    If (TryCast(Client, UktEthernetConnect) IsNot Nothing) Then
                        CType(Client, UktEthernetConnect).SetParamaters(Hostname, Port)
                    End If
                End If
            End Set
        End Property
        Private _Hostname As String = "192.168.0.10"

        Public Property Port As Integer
            Get
                Return _Port
            End Get
            Set(value As Integer)
                If (_Port <> value) Then
                    _Port = value
                    NotifyPropertyChanged(NameOf(Port))
                    If (TryCast(Client, UktEthernetConnect) IsNot Nothing) Then
                        CType(Client, UktEthernetConnect).SetParamaters(Hostname, Port)
                    End If
                End If
            End Set
        End Property
        Private _Port As Integer = 4002

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
                        Case Connection.Uart
                            Client = New UktUartConnect()
                            Client.SetParamaters(PortName)
                        Case Connection.Eth
                            Client = New UktEthernetConnect()
                            Client.SetParamaters(Hostname, Port)
                    End Select
                End If
            End Set
        End Property
        Private _Connection As Connection = Connection.None

        Public Overrides ReadOnly Property ConnectionsDict As New Dictionary(Of Connection, String) From {
            {Connection.Eth, "Ethernet"},
            {Connection.Uart, "Uart"}
        }

#End Region '/PROPS

#Region "METHODS"

        ''' <summary>
        ''' Разбирает 19 байтов сырых данных от прибора, заполняя 8 каналов температуры.
        ''' </summary>
        Public Shared Function ParseTemperature(bytes As IEnumerable(Of Byte)) As Double?()
            If (bytes.Count < 19) Then
                If (Language.Name = "ru-RU") Then
                    Throw New ArgumentException("Получено некорректное число байтов температуры")
                Else
                    Throw New ArgumentException("Incorrect amount of data bytes")
                End If
            End If

            Dim q As New Queue(Of Byte)
            For Each b In bytes
                q.Enqueue(b)
            Next

            Dim temperature As Double?() = New Double?(Ukt38.NUM_CHANNELS - 1) {}
            If (q.Count > 0) Then
                'вычитываем до маркера начала пакета
                Dim by As Byte
                Do
                    by = q.Dequeue()
                Loop Until (by = Ukt38.Mark.StartSession)

                'следуюший байт - признак готовности прибора
                Dim rdy As Ukt38.Mark = CType(q.Dequeue(), Mark)
                If (rdy <> Mark.DeviceReady) Then
                    If (Language.Name = "ru-RU") Then
                        Throw New Exception("Прибор не готов")
                    Else
                        Throw New Exception("Device not ready")
                    End If
                End If

                'следующий байт - длина чтения
                Dim len As Ukt38.ReadLen = CType(q.Dequeue(), ReadLen)
                If (len <> ReadLen.READ_16_BYTE) Then
                    If (Language.Name = "ru-RU") Then
                        Throw New Exception("Вернулось некорректное число данных")
                    Else
                        Throw New Exception("Incorrect data bytes amount")
                    End If
                End If

                'следующий байт - адрес регистра
                Dim reg As Ukt38.Registers = CType(q.Dequeue(), Registers)
                If (reg <> Registers.Temp) Then
                    If (Language.Name = "ru-RU") Then
                        Throw New Exception("Данные пришли не из регистра температуры")
                    Else
                        Throw New Exception("Data received from wrong register")
                    End If
                End If

                'далее по 2 байта идут значения 8-ми каналов температуры:
                For index As Integer = 0 To Ukt38.NUM_CHANNELS - 1
                    Dim low As UShort = q.Dequeue()
                    Dim hi As UShort = q.Dequeue()
                    Dim value As UShort = (hi << 8) Or low
                    If (value <> ChannelState.Err) AndAlso (value <> ChannelState.Off) Then
                        temperature(index) = GetTemperatureByCode(value)
                    Else
                        temperature(index) = Nothing
                    End If
                Next
            End If
            Return temperature
        End Function

        ''' <summary>
        ''' Перевод из допкода в градусы Цельсия.
        ''' </summary>
        Public Shared Function GetTemperatureByCode(code As UShort?) As Double?
            If code.HasValue Then
                Dim value As Double
                If ((code >> 15) = 1) Then 'отрицательное число
                    value = code.Value - &HFFFF
                Else
                    value = code.Value
                End If
                Return value * 0.1
            End If
            Return Nothing
        End Function

#End Region '/METHODS

#Region "NESTED TYPES"

        ''' <summary>
        ''' Длины для чтения из прибора.
        ''' </summary>
        Public Enum ReadLen As Byte
            READ_BYTE = 1
            READ_WORD = 2
            READ_16_BYTE = 3
            READ_32_BYTE = 4
        End Enum

        ''' <summary>
        ''' Начальные адреса регистров.
        ''' </summary>
        Public Enum Registers As Byte
            ''' <summary>
            ''' A0..AF - Температуры каналов по 2 байта целое в дополнительном коде до 0.1°C или до 1°С.
            ''' </summary>
            ''' <remarks>
            ''' В аварийном канале показания 0xAAAA.
            ''' В отключенном (или еще неизмеренном) канале показания 0xBBBB.
            ''' </remarks>
            Temp = &HA0
            ''' <summary>
            ''' B0…BF - Уставки каналов по 2 байта целое в дополнительном коде до 0.1°С или до 1°С.
            ''' </summary>
            ''' <remarks>
            ''' В отключенном (или еще неизмеренном) канале уставка равна 0xBBBB.
            ''' </remarks>
            Setting = &HB0
            ''' <summary>
            ''' C0…CF - Дельты каналов по 2 байта целое в дополнительном коде до 0.1°С или до 1°С.
            ''' </summary>
            ''' <remarks>
            ''' В отключенном (или еще неизмеренном) канале уставка равна 0xBBBB.
            ''' </remarks>
            Delta = &HC0
            ''' <summary>
            ''' D0…D7 - Положение десятичной точки в канале.
            ''' Определяет степень «10» (5-значение из адреса), на которое нужно делить получаемое целое число температуры, уставки или дельты. 
            ''' </summary>
            DecimalPointPosition = &HD0
        End Enum

        ''' <summary>
        ''' Признаки.
        ''' </summary>
        Public Enum Mark As Byte
            StartSession = &H71
            DeviceReady = &H55
        End Enum

        ''' <summary>
        ''' Состояния каналов.
        ''' </summary>
        Public Enum ChannelState As UShort
            OK = 1
            Err = &HAAAA 'авария
            Off = &HBBBB 'выключен или ещё не измерен
        End Enum

#End Region '/NESTED TYPES

    End Class

End Namespace
