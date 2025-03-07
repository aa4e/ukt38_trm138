Imports System.Collections.ObjectModel
Imports System.ComponentModel

Namespace OWEN

    ''' <summary>
    ''' Базовый класс устройств фирмы ОВЕН.
    ''' </summary>
    Public MustInherit Class DeviceBase
        Implements INotifyPropertyChanged

        Public Const NUM_CHANNELS As Integer = 8

        Protected Sub New()
            AddHandler QueryServerCompleted, AddressOf QueryServerCompletedHandler
            AddHandler Measurements.CollectionChanged, Sub() Debug.WriteLine($"col chngd, len={Measurements.Count}") 'NotifyPropertyChanged(NameOf(Measurements))
        End Sub

        Public Shared Property CurDevType As DeviceType = DeviceType.Ukt38
        Public MustOverride ReadOnly Property DeviceType As DeviceType

        ''' <summary>
        ''' Имя COM-порта.
        ''' </summary>
        Public Property PortName As String
            Get
                Return _PortName
            End Get
            Set(value As String)
                If (value IsNot Nothing) AndAlso (value.Length > 0) AndAlso (_PortName <> value) Then
                    _PortName = value
                    NotifyPropertyChanged(NameOf(PortName))
                    If (TryCast(Client, UktUartConnect) IsNot Nothing) Then
                        CType(Client, UktUartConnect).SetParamaters(PortName)
                    End If
                End If
            End Set
        End Property
        Private _PortName As String = "COM1"

        Public Property MeasureInProcess As Boolean
            Get
                Return _MeasureInProcess
            End Get
            Private Set(value As Boolean)
                If (_MeasureInProcess <> value) Then
                    _MeasureInProcess = value
                    NotifyPropertyChanged(NameOf(MeasureInProcess))
                End If
            End Set
        End Property
        Private _MeasureInProcess As Boolean = False

        ''' <summary>
        ''' Период опроса, сек.
        ''' </summary>
        Public Property Period As Integer
            Get
                Return _Period
            End Get
            Set(value As Integer)
                If (_Period <> value) AndAlso (value > 1) Then
                    _Period = value
                    NotifyPropertyChanged(NameOf(Period))
                End If
            End Set
        End Property
        Private _Period As Integer = 10

        ''' <summary>
        ''' Базовый адрес прибора.
        ''' </summary>
        Public Property BaseAddress As Integer
            Get
                Return _BaseAddress
            End Get
            Set(value As Integer)
                If (_BaseAddress <> value) AndAlso (value > 1) Then
                    _BaseAddress = value
                    NotifyPropertyChanged(NameOf(BaseAddress))
                End If
            End Set
        End Property
        Private _BaseAddress As Integer = 16

        ''' <summary>
        ''' Измерения темепературы.
        ''' </summary>
        Public ReadOnly Property Measurements As New ObservableRangeCollection(Of TempMeasure)

        Public Property Status As String
            Get
                Return _Status
            End Get
            Private Set(value As String)
                If (_Status <> value) Then
                    _Status = value
                    NotifyPropertyChanged(NameOf(Status))
                End If
            End Set
        End Property
        Private _Status As String = ""

        Public MustOverride ReadOnly Property ConnectionsDict As Dictionary(Of Connection, String)
        Public MustOverride Property Connection As Connection
        Protected Client As IConnect

        Public Shared Property Language As New Globalization.CultureInfo("ru-RU")

        ''' <summary>
        ''' Возвращает длину в байтах по значению из перечисления.
        ''' </summary>
        Public Shared ReadOnly Property LengthInBytes As New Dictionary(Of Ukt38.ReadLen, Integer) From {
            {Ukt38.ReadLen.READ_16_BYTE, 16},
            {Ukt38.ReadLen.READ_32_BYTE, 32},
            {Ukt38.ReadLen.READ_WORD, 2},
            {Ukt38.ReadLen.READ_BYTE, 1}
        }

        Public ReadOnly Property DeviceTypesDict As Dictionary(Of DeviceType, String)
            Get
                If Language.Name = "ru-RU" Then
                    Return New Dictionary(Of DeviceType, String) From {{DeviceType.Ukt38, "УКТ38"}, {DeviceType.Trm138, "ТРМ138"}}
                Else
                    Return New Dictionary(Of DeviceType, String) From {{DeviceType.Ukt38, "UKT38"}, {DeviceType.Trm138, "TRM138"}}
                End If
            End Get
        End Property

        Public Event QueryServerCompleted As EventHandler(Of QueryServerCompletedEventArgs)
        Private AsyncOperation As AsyncOperation = Nothing
        Private Delegate Sub QueryServerDelegate()
        Private WithEvents MeasTimer As New Timers.Timer()
        Private IsBusy As Boolean

#Region "METHODS"

        ''' <summary>
        ''' Запуск/останов опроса датчика.
        ''' </summary>
        Public Function ToggleMeasurements() As Boolean
            If MeasureInProcess Then
                MeasureInProcess = False
                MeasTimer.Stop()
                Client.Close()
                If (Language.Name = "ru-RU") Then
                    Status = $"{Now} Опрос завершён."
                Else
                    Status = $"{Now} Stopped."
                End If
            Else
                Measurements.Clear()
                TempMeasure.ResetStaticProps()
                MeasureInProcess = True
                MeasTimer.Interval = 10 'чтобы запустить сразу
                MeasTimer.Start()
                If (Language.Name = "ru-RU") Then
                    Status = $"{Now} Опрос начат."
                Else
                    Status = $"{Now} Started."
                End If
            End If
            Return MeasureInProcess
        End Function

        ''' <summary>
        ''' Проводит измерение по таймеру.
        ''' </summary>
        Private Sub MakeMeasure(sender As Object, e As Timers.ElapsedEventArgs) Handles MeasTimer.Elapsed
            If (Not IsBusy) Then
                IsBusy = True
                AsyncOperation = AsyncOperationManager.CreateOperation(Nothing)
                Dim d As New QueryServerDelegate(AddressOf QueryServerAsync)
                Dim ar = d.BeginInvoke(Nothing, Nothing)
            End If
        End Sub

        Private Sub QueryServerAsync()
            SyncLock (Me)
                Dim e As QueryServerCompletedEventArgs = QueryServer()
                Dim opc As New System.Threading.SendOrPostCallback(AddressOf AsyncOperationCompleted)
                AsyncOperation.PostOperationCompleted(opc, e)
            End SyncLock
        End Sub

        ''' <summary>
        ''' Отправляет синхронный запрос к серверу и получает ответ.
        ''' </summary>
        Private Function QueryServer() As QueryServerCompletedEventArgs
            Dim result As New QueryServerCompletedEventArgs()
            Try
                If (Not Client.IsOpen) Then
                    Client.Open()
                End If

                Try
                    Dim temps As Double?() = Client.ReadTemperature()
                    Dim meas As New TempMeasure(Now, temps)

                    result.Data = meas
                    result.Succeeded = True

                    Measurements.Add(meas)

                Catch ex As Exception
                    result.Succeeded = False
                    result.ErrorData = New ErrorData(ex.Message)
                End Try

            Catch ex As Exception
                result.ErrorData = New ErrorData(ex)
            End Try

            Return result

        End Function

        ''' <summary>
        ''' Обработчик получения ответа от сервера.
        ''' </summary>
        Private Sub QueryServerCompletedHandler(sender As Object, e As QueryServerCompletedEventArgs)
            If (Not e.ErrorData.HasError) Then
                If (Language.Name = "ru-RU") Then
                    Status = $"{Now} Получен ответ прибора."
                Else
                    Status = $"{Now} Packet received."
                End If
            Else
                If (Language.Name = "ru-RU") Then
                    Status = $"{Now} Ошибка обмена: {e.ErrorData.ErrorText}"
                Else
                    Status = $"{Now} Connection error: {e.ErrorData.ErrorText}"
                End If
            End If
            MeasTimer.Interval = Period * 1000
        End Sub

        ''' <summary>
        ''' Разбирает журнал и заполняет массив измерений <see cref="Measurements"/>.
        ''' </summary>
        Public Sub ParseLog(fileName As String)
            Measurements.Clear()
            TempMeasure.ResetStaticProps()

            Dim fi As New IO.FileInfo(fileName)
            Dim msmnts As New List(Of TempMeasure)
            Using fs As New IO.FileStream(fi.FullName, IO.FileMode.Open, IO.FileAccess.Read), sr As New IO.StreamReader(fs)
                Do While (sr.Peek <> -1)
                    Dim line As String = sr.ReadLine()
                    Dim m As New TempMeasure(line)
                    msmnts.Add(m)
                Loop
            End Using

            Measurements.AddRange(msmnts)

            If (Language.Name = "ru-RU") Then
                Status = $"Обработка файла [{fi.Name}] завершена. Найдено измерений: {Measurements.Count}."
            Else
                Status = $"File [{fi.Name}] processed. Measurements found: {Measurements.Count}."
            End If
        End Sub

        ''' <summary>
        ''' Сохраняет журнал.
        ''' </summary>
        Public Sub SaveLog(fileName As String)
            Using fs As New IO.FileStream(fileName, IO.FileMode.Create, IO.FileAccess.Write), sw As New IO.StreamWriter(fs)
                For i As Integer = 0 To Measurements.Count - 1
                    sw.WriteLine(Measurements(i).ToString())
                Next
            End Using
            If (Language.Name = "ru-RU") Then
                Status = $"{Now} Журнал [ {fileName} ] сохранён."
            Else
                Status = $"{Now} Log [ {fileName} ] saved."
            End If
        End Sub

        Private Sub AsyncOperationCompleted(arg As Object)
            IsBusy = False
            If (QueryServerCompletedEvent IsNot Nothing) Then
                RaiseEvent QueryServerCompleted(Me, DirectCast(arg, QueryServerCompletedEventArgs))
            End If
        End Sub

#End Region '/METHODS

#Region "INOTIFY"

        Public Event PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged
        Protected Sub NotifyPropertyChanged(propName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
        End Sub

#End Region '/INOTIFY

    End Class

End Namespace
