Imports NServiceBus

Public Class VBMultiTestSaga
    Inherits Saga(Of VBMultiTestSaga.SagaData)

    Public Class SagaData
        Inherits ContainSagaData

        Public Property Correlation As String
    End Class

    Protected Overrides Sub ConfigureHowToFindSaga(ByVal mapper As SagaPropertyMapper(Of SagaData))
        mapper.MapSaga(Function(saga) saga.Correlation).ToMessage(Of MessageA)(Function(msg) msg.Correlation).ToMessage(Of MessageD)(Function(msg) msg.DifferentName).ToMessage(Of Messagec)(Function(msg) msg.Part1 + msg.Part2 + $"{msg.Part1}{msg.Part2}" + String.Format("{0}{1}", msg.Part1, msg.Part2))
    End Sub
End Class