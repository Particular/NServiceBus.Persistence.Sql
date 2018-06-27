Imports NServiceBus

Public Class VBMultiTestSaga
    Inherits Saga(Of VBMultiTestSaga.SagaData)

    Public Class SagaData
        Inherits ContainSagaData

        Public Property Correlation As String
    End Class

    Protected Overrides Sub ConfigureHowToFindSaga(ByVal mapper As SagaPropertyMapper(Of SagaData))
        mapper.ConfigureMapping(Of MessageA)(Function(msg) msg.Correlation).ToSaga(Function(saga) saga.Correlation)
        mapper.ConfigureMapping(Of MessageD)(Function(msg) msg.DifferentName).ToSaga(Function(saga) saga.Correlation)
        mapper.ConfigureMapping(Of MessageC)(Function(msg) msg.Part1 + msg.Part2 + $"{msg.Part1}{msg.Part2}" + String.Format("{0}{1}", msg.Part1, msg.Part2)) _
            .ToSaga(Function(saga) saga.Correlation)
    End Sub
End Class