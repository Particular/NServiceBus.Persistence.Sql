Imports NServiceBus

Public Class MessageA
    Implements ICommand

    Public Property Correlation As String
End Class

Public Class MessageB
    Implements ICommand

    Public Property Correlation As String
End Class

Public Class MessageC
    Implements ICommand

    Public Property Part1 As String
    Public Property Part2 As String
End Class

Public Class MessageD
    Implements ICommand

    Public Property DifferentName As String
End Class