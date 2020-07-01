Option Explicit On

Public Class Form1

    Dim app As Object
    Dim model As Object
    Dim le As Object

    ' spinning control
    Dim pattern As String
    Dim refElement As Integer
    Dim sstep As Integer
    Dim numberOfElement As Integer
    Dim beamNumber As Integer
    Dim totalElementNumber As Integer

    Dim refPosition As _refPosition
    Dim refType As _refType

    Dim rangeList As New List(Of List(Of Integer))

    Dim _refFile As String

    Dim infoText As String

    Public Enum _refPosition
        middle
        boundary_left
    End Enum

    Public Enum _refType
        _on
        off
    End Enum

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        app = CreateObject("CSTStudio.Application")

        For beam As Integer = 1 To beamNumber

            updateInfoText("Simulating project for beam " + beam.ToString)

            model = app.OpenFile(_refFile + "_beam-" + beam.ToString + ".cst")

            Dim ret As Integer

            ret = model.Solver.Start()

            If (ret = 1) Then
                updateInfoText("Simulation for beam " + beam.ToString + " has succeeded")
            Else
                updateInfoText("Simulation for beam " + beam.ToString + " has failed")
            End If

            model.Save()

            model.Quit()
        Next
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged

        Select Case ComboBox1.SelectedItem
            Case "Middle"
                refPosition = _refPosition.middle
            Case "Left boundary"
                refPosition = _refPosition.boundary_left
        End Select
    End Sub

    Private Sub NumericUpDown2_ValueChanged(sender As Object, e As EventArgs) Handles NumericUpDown2.ValueChanged
        numberOfElement = NumericUpDown2.Value
    End Sub

    Private Sub NumericUpDown1_ValueChanged(sender As Object, e As EventArgs) Handles NumericUpDown1.ValueChanged
        sstep = NumericUpDown1.Value
    End Sub

    Private Sub generateProject_old()

        Dim refFile As String
        refFile = "D:\limos\port-test-2\vb\wire\test"

        app = CreateObject("CSTStudio.Application")

        generateRanges()

        For beam As Integer = 0 To 0 'beamnumber



            model = app.OpenFile(refFile + ".cst")

            Dim currentRange As List(Of Integer)
            currentRange = rangeList.Item(beam)

            Dim i As Long

            Try
                i = model.LumpedElement.StartLumpedElementNameIteration()
            Catch
            End Try

            MsgBox(i)

            Dim le As Integer
            For le = 0 To i
                MsgBox("Modifying element " + le.ToString)
                If (currentRange.Contains(le)) Then
                    MsgBox("Yes")

                    ' modify element as ON
                    model.LumpedElement.Reset()
                    model.LumpedElement.SetName(le.ToString)
                    model.LumpedElement.setR(0)
                    model.LumpedElement.setL(5)
                    model.LumpedElement.setC(0)
                    model.LumpedElement.Modify()

                End If

                Try
                    'modify element as OFF
                    model.LumpedElement.Reset()
                    model.LumpedElement.SetName(le.ToString)
                    model.LumpedElement.SetR(3000)
                    model.LumpedElement.SetL(0)
                    model.LumpedElement.SetC(0)
                    model.LumpedElement.Modify()
                Catch
                End Try

            Next

            model.SaveAs(refFile + "-beam.cst", False)
            model.Save()
            'model.Quit()
        Next

    End Sub

    Private Sub generateProjectFiles()

        Dim newFile As String

        generateRanges()

        app = CreateObject("CSTStudio.Application")

        For beam As Integer = 1 To beamNumber

            updateInfoText("Generating project for beam " + beam.ToString)

            model = app.OpenFile(_refFile + ".cst")

            newFile = _refFile + "_beam-" + beam.ToString + ".cst"
            model.SaveAs(newFile, False)

            model.Quit()

            processProject(_refFile + "_beam-" + beam.ToString)
        Next

    End Sub

    Private Sub processProject(dir)
        ' generate command
        Dim argPart As String
        argPart = ""
        'If (refType = _refType.off) Then
        'argPart += "--opened"

        'get range
        'Else
        'argPart += "--off"
        'End If

        argPart += " --opened "

        Dim cList As List(Of Integer)
        cList = rangeList(0)

        Dim _acc As String = ""
        For Each el As Integer In cList
            ' fixed pattern prefix LE_
            _acc += "LE_" + el.ToString + ","
        Next
        rangeList.RemoveAt(0)

        argPart += _acc

        argPart += " --on r=" + ON_R_Input.Text + ",l=" + ON_L_Input.Text + ",c=" + ON_C_Input.Text + " --off r=" + OFF_R_Input.Text + ",l=" + OFF_L_Input.Text + ",c=" + OFF_C_Input.Text


        Dim myProcess As Process = New Process()

        myProcess.StartInfo.FileName = "node"
        myProcess.StartInfo.Arguments = "./modify.js --dir " + dir + argPart
        myProcess.Start()

        myProcess.WaitForExit()

        If (myProcess.ExitCode <> 0) Then
            updateInfoText("ERROR processing model file")
        Else
            updateInfoText("SUCCESS processing model file")
        End If

        updateInfoText(argPart)


    End Sub

    Private Sub generateRanges()

        rangeList.Clear()
        refElement = refElementValueInput.Value

        For beam As Integer = 1 To beamNumber
            Dim list As New List(Of Integer)

            updateInfoText("Generating range for beam " + beam.ToString)

            list.Add(refElement)

            If (refPosition = _refPosition.middle) Then
                updateInfoText("... ref element is " + refElement.ToString)
                For onElem As Integer = 1 To (numberOfElement - 1) / 2
                    Dim t As Integer
                    t = ((refElement - onElem) Mod totalElementNumber)
                    If (t < 0) Then
                        t += totalElementNumber
                    End If

                    list.Add(t)

                Next
                For onElem As Integer = 1 To (numberOfElement - 1) / 2
                    Dim t As Integer
                    t = ((refElement + onElem) Mod totalElementNumber)
                    list.Add(t)
                Next

            ElseIf (refPosition = _refPosition.boundary_left) Then
                updateInfoText("... ref element is " + refElement.ToString)
                For onElem As Integer = 1 To (numberOfElement - 1)
                    Dim t As Integer
                    t = ((refElement - onElem) Mod totalElementNumber)
                    If (t < 0) Then
                        t += totalElementNumber
                    End If

                    list.Add(t)

                Next
            End If

            'Print range list
            Dim _acc As String = "["
            For Each index As Integer In list
                _acc += index.ToString + ","
            Next
            _acc += "]"
            updateInfoText("... Opened elements : " + _acc)
            ' save to rangeList
            rangeList.Add(list)

            'next
            refElement = (refElement + sstep) Mod totalElementNumber
        Next

        updateInfoText("Finished")

    End Sub

    Private Sub NumericUpDown3_ValueChanged(sender As Object, e As EventArgs) Handles NumericUpDown3.ValueChanged
        beamNumber = NumericUpDown3.Value
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        generateRanges()

    End Sub

    Private Sub NumericUpDown4_ValueChanged(sender As Object, e As EventArgs) Handles NumericUpDown4.ValueChanged
        totalElementNumber = NumericUpDown4.Value
    End Sub

    Private Sub refElementValueInput_ValueChanged(sender As Object, e As EventArgs) Handles refElementValueInput.ValueChanged
        refElement = refElementValueInput.Value
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        generateProjectFiles()
    End Sub

    Private Sub updateInfoText(textPart)
        infoText += textPart + Environment.NewLine

        InfoBox.Text = infoText
    End Sub

    Private Sub OpenFileDialog1_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles OpenProjectDialog.FileOk
        _refFile = OpenProjectDialog.FileName
        Dim pdir, fname As String
        pdir = IO.Path.GetDirectoryName(_refFile)
        fname = IO.Path.GetFileNameWithoutExtension(_refFile)

        _refFile = IO.Path.Combine(pdir, fname)

        updateInfoText("Selected project " + _refFile)
    End Sub

    Private Sub OpenFileDialogButton_Click(sender As Object, e As EventArgs) Handles OpenFileDialogButton.Click
        Me.OpenProjectDialog.ShowDialog()
    End Sub

End Class
