Namespace OWEN

    ''' <summary>
    ''' Инфраструктурный интерфейс для обмена с прибором ОВЕН.
    ''' </summary>
    Public Interface IConnect

        ''' <summary>
        ''' Открыто ли соединение.
        ''' </summary>
        ReadOnly Property IsOpen As Boolean

        ''' <summary>
        ''' Открывает соединение и возвращает успех/провал.
        ''' </summary>
        Function Open() As Boolean

        ''' <summary>
        ''' Закрывает соединение.
        ''' </summary>
        Function Close() As Boolean

        ''' <summary>
        ''' Читает значения температуры со всех каналов прибора.
        ''' </summary>
        ''' <returns>
        ''' Неактивные каналы возвращают Null, активные - значение в градусах Цельсия.
        ''' </returns>
        Function ReadTemperature() As Double?()

        ''' <summary>
        ''' Задаёт параметры интерфейса.
        ''' </summary>
        Sub SetParamaters(ParamArray params As Object())

    End Interface

End Namespace
