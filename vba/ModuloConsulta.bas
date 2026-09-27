Attribute VB_Name = "ModuloConsulta"
Option Explicit

Public Sub ExecutarConsultaPython()

    Dim pastaProjeto As String
    Dim caminhoPython As String
    Dim arquivoPythonConfigurado As String
    Dim pastaSaida As String
    Dim scriptPython As String
    Dim arquivoManifesto As String
    Dim arquivoErro As String
    Dim comando As String
    Dim shell As Object
    Dim codigoSaida As Long
    Dim mensagemPython As String
    Dim planilhaControle As Worksheet

    On Error GoTo TratarErro

    pastaProjeto = ThisWorkbook.Path
    arquivoPythonConfigurado = pastaProjeto & _
        "\runtime\python-executavel.txt"

    If Dir(arquivoPythonConfigurado) <> "" Then
        caminhoPython = Trim$(LerArquivoUTF8(arquivoPythonConfigurado))
    Else
        caminhoPython = Environ$("LOCALAPPDATA") & _
            "\Programs\Python\Python312\python.exe"
    End If

    pastaSaida = pastaProjeto & "\saida"
    scriptPython = pastaProjeto & "\python\consulta.py"
    arquivoManifesto = pastaSaida & "\manifesto.csv"
    arquivoErro = pastaSaida & "\erro_consulta.txt"

    If Dir(caminhoPython) = "" Then
        Err.Raise vbObjectError + 1, , _
            "Python não encontrado em: " & caminhoPython
    End If

    If Dir(scriptPython) = "" Then
        Err.Raise vbObjectError + 2, , _
            "Script não encontrado em: " & scriptPython
    End If

    Application.ScreenUpdating = False
    Application.StatusBar = "Executando consulta..."

    comando = """" & caminhoPython & """ " & _
              """" & scriptPython & """ " & _
              """" & pastaSaida & """"

    Set shell = CreateObject("WScript.Shell")
    codigoSaida = shell.Run(comando, 0, True)

    If codigoSaida <> 0 Then
        If Dir(arquivoErro) <> "" Then
            mensagemPython = LerArquivoUTF8(arquivoErro)
        Else
            mensagemPython = "O Python terminou com o código " & codigoSaida
        End If

        Err.Raise vbObjectError + 3, , _
            mensagemPython
    End If

    If Dir(arquivoManifesto) = "" Then
        Err.Raise vbObjectError + 4, , _
            "O Python não gerou o arquivo manifesto.csv."
    End If

    ImportarArquivosDoManifesto arquivoManifesto, pastaSaida

    Set planilhaControle = ThisWorkbook.Worksheets( _
        "Distribui" & ChrW(231) & ChrW(227) & "o")

    With planilhaControle.Range("E8")
        .Value = Now
        .NumberFormat = "dd/mm/yyyy hh:mm:ss"
    End With

    Application.StatusBar = False
    Application.ScreenUpdating = True

    Exit Sub

TratarErro:
    Application.StatusBar = False
    Application.ScreenUpdating = True

    MsgBox _
        "Não foi possível executar a consulta." & vbCrLf & _
        Err.Description, _
        vbCritical

End Sub


Private Function LerArquivoUTF8(ByVal caminhoArquivo As String) As String

    Dim stream As Object

    Set stream = CreateObject("ADODB.Stream")

    With stream
        .Type = 2
        .Charset = "utf-8"
        .Open
        .LoadFromFile caminhoArquivo
        LerArquivoUTF8 = .ReadText
        .Close
    End With

End Function


Private Sub ImportarArquivosDoManifesto( _
    ByVal caminhoManifesto As String, _
    ByVal pastaProjeto As String)

    Dim stream As Object
    Dim texto As String
    Dim linhas As Variant
    Dim campos As Variant
    Dim indice As Long
    Dim nomeArquivo As String
    Dim nomeAba As String
    Dim caminhoArquivo As String
    Dim planilhaDestino As Worksheet

    Set stream = CreateObject("ADODB.Stream")

    With stream
        .Type = 2
        .Charset = "utf-8"
        .Open
        .LoadFromFile caminhoManifesto
        texto = .ReadText
        .Close
    End With

    linhas = Split(Replace(texto, vbCr, ""), vbLf)

    For indice = 1 To UBound(linhas)
        If Len(Trim$(linhas(indice))) > 0 Then
            campos = Split(linhas(indice), ";")

            If UBound(campos) < 1 Then
                Err.Raise vbObjectError + 5, , _
                    "Linha inválida no manifesto: " & linhas(indice)
            End If

            nomeArquivo = Trim$(campos(0))
            nomeAba = Trim$(campos(1))
            caminhoArquivo = pastaProjeto & "\" & nomeArquivo

            If Dir(caminhoArquivo) = "" Then
                Err.Raise vbObjectError + 6, , _
                    "Arquivo indicado no manifesto não encontrado: " & caminhoArquivo
            End If

            Set planilhaDestino = ObterOuCriarAba(nomeAba)
            ImportarCSV caminhoArquivo, planilhaDestino
        End If
    Next indice

End Sub


Private Function ObterOuCriarAba(ByVal nomeAba As String) As Worksheet

    On Error Resume Next
    Set ObterOuCriarAba = ThisWorkbook.Worksheets(nomeAba)
    On Error GoTo 0

    If ObterOuCriarAba Is Nothing Then
        Set ObterOuCriarAba = ThisWorkbook.Worksheets.Add( _
            After:=ThisWorkbook.Worksheets(ThisWorkbook.Worksheets.Count))
        ObterOuCriarAba.Name = nomeAba
    End If

End Function


Private Sub ImportarCSV( _
    ByVal caminhoCSV As String, _
    ByVal planilhaDestino As Worksheet)

    Dim consulta As QueryTable

    planilhaDestino.Cells.Clear

    Set consulta = planilhaDestino.QueryTables.Add( _
        Connection:="TEXT;" & caminhoCSV, _
        Destination:=planilhaDestino.Range("A1"))

    With consulta
        .TextFilePlatform = 65001
        .TextFileParseType = xlDelimited
        .TextFileSemicolonDelimiter = True
        .TextFileDecimalSeparator = ","
        .TextFileThousandsSeparator = "."
        .AdjustColumnWidth = True
        .RefreshStyle = xlOverwriteCells
        .Refresh BackgroundQuery:=False
        .Delete
    End With

    planilhaDestino.Rows(1).Font.Bold = True
    planilhaDestino.Columns.AutoFit

End Sub

