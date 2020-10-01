Imports System.IO
Imports System.Net
Imports System.Net.Mail

Public Class Form1

    Public dataDirectory As String = My.Application.Info.DirectoryPath

    Public timeSniffing As Integer
    Public sniffInterval As Integer
    Public sniffIntervalMinimum As Integer = 3
    Public LastSniffed As Integer

    Public sniffing As Boolean = False
    Public objectsFound As String = ""

    Public newLine As String = vbCrLf

    Private passwordGmailSave As String

    Public webClient As System.Net.WebClient

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        'urlBox1.Text = "https://www.naturaselection.com/es/tienda/ropa/coleccion-organica"
        'urlBox1.Text = "https://www.nike.com/es/ca/home"
        'urlBox1.Text = "https://es.louisvuitton.com/esp-es/hombre/carteras-y-pequena-marroquineria/todas-las-carteras-y-pequena-marroquineria/_/N-5ts7t2"

        LastSniffed = 0
        runningTimelbl.Text = "00:00:00"
        Button2.Text = "Start"
        CheckBox1.Checked = False
        StopSniffing()

    End Sub
    'Use in case you want to keep the same connexion for every interval, not working tho
    Private Sub SetWebConnexion()
        webClient = New System.Net.WebClient()
        webClient.UseDefaultCredentials = True
        webClient.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials
        webClient.Headers.Set("User-Agent", "Mozilla/5.0 (Linux; <Android Version>; <Build Tag etc.>) AppleWebKit/<WebKit Rev> (KHTML, like Gecko) Chrome/<Chrome Rev> Mobile Safari/<WebKit Rev>")
        'webClient.Headers.Set(HttpRequestHeader.KeepAlive, "300")
        'webClient.Headers.Set(HttpRequestHeader.Connection, "keep-alive")
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick

        If (TextBox1.Text = "") Then
            TextBox1.Text = sniffIntervalMinimum.ToString
        End If

        'Sets a minimuminterval time for the system can complete the html search between sniffs
        sniffInterval = Math.Max(Convert.ToInt32(TextBox1.Text), sniffIntervalMinimum)
        TextBox1.Text = sniffInterval.ToString

        If sniffing Then
            timeSniffing += 1
            runningTimelbl.Text = SecondsToTime(timeSniffing)

            If timeSniffing - LastSniffed >= sniffInterval Then
                Sniff()
                LastSniffed = timeSniffing

            End If
        End If


    End Sub

    Private Sub Sniff()
        AnalyzeHTML()
    End Sub

    Private Sub GetHTML()
        Dim request As WebRequest = WebRequest.Create(urlBox1.Text)
        request.UseDefaultCredentials = True
        request.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials

        Using response As WebResponse = request.GetResponse()
            Using reader As New StreamReader(response.GetResponseStream())
                Dim html As String = reader.ReadToEnd()
                File.WriteAllText(dataDirectory + "\page.html", html)
            End Using
        End Using
    End Sub


    Private Sub AnalyzeHTML()
        If (urlBox1.Text = "") Then
            StopSniffing()
            MsgBox("Web url empty. Sniffing will not start")

            Return
        End If

        webClient = New System.Net.WebClient()
        webClient.UseDefaultCredentials = True
        webClient.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials
        webClient.Headers.Set("User-Agent", "Mozilla/5.0 (Linux; <Android Version>; <Build Tag etc.>) AppleWebKit/<WebKit Rev> (KHTML, like Gecko) Chrome/<Chrome Rev> Mobile Safari/<WebKit Rev>")

        'sense aquest header LV no m'acceptava
        webClient.Headers.Add("Accept", "text / html, application / xhtml + xml, image / jxr, */*")

        webClient.Headers.Add("Accept-Language", "pt - PT, pt; q = 0.8, en - US; q = 0.5, en; q = 0.3")
        webClient.Headers.Add("Cookie", "_ga = GA1.3.1173403303.1470761279; tw =% 7B % 22browse % 22 % 3A1134 % 7D; userinfo = __01658d30ffdd0b479894 % 3B % 7B % 22username % 22 % 3A % 22 % 22 % 2C % 22uniqueid % 22 % 3A % 22a5dc80fe2a7aa790049fced4be5d0f39 % 22 % 2C % 22vd % 22 % 3A % 22BXqgk9 % 2CBYJFwd % 2CA % 2CS % 2CA % 2C % 2CL % 2CB % 2CB % 2CBYJJOx % 2CBYJJhI % 2CO % 2CD % 2CJ % 2CBYJJhI % 2C13 % 2CA % 2CB % 2CA % 2C % 2CB % 2CA % 2CB % 2CA % 2CA % 2C % 2CA % 22 % 2C % 22attr % 22 % 3A67108880 % 7D; _ga = GA1.2.1173403303.1470761279; __qca = P0 - 724951276 - 1470761281462; __gads = ID = f00ea51d533ffef6:T = 1472910412:S = ALNI_MZThZH7YlaHm_VKaQ_TH2Hik7nsmQ; _gat = 1")
        webClient.Headers.Add("DNT", "1")

        Dim sourceString As String = ""

        Try
            sourceString = webClient.DownloadString(urlBox1.Text)
        Catch ex As Exception
            StopSniffing()
            MsgBox(ex)
        End Try


        Dim keyWords() As String = wordsToFindtxt.Text.Split(New String() {Environment.NewLine},
                                       StringSplitOptions.None)
        Dim resultText As String = ""

        Dim haTrobat As Boolean = False
        objectsFound = ""

        For Each keyWord In keyWords
            If keyWord <> "" Then
                If sourceString.ToLower.Contains(keyWord.ToLower) Then
                    resultText += "Trobat: " + keyWord + vbCrLf

                    objectsFound += keyWord + ","
                    haTrobat = True
                Else
                    resultText += "No trobat: " + keyWord + vbCrLf
                End If
            End If
        Next


        resultText += "Sniffed at: " + Now
        Label3.Text = resultText

        If (haTrobat And CheckBox1.Checked) Then
            SendEmail()
            StopSniffing()
        End If


    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        'Sniff()
        GetHTML()
    End Sub

    Private Function SecondsToTime(ByVal seconds) As String

        Dim mHours As Long, mMinutes As Long, mSeconds As Long
        mSeconds = seconds
        mHours = mSeconds \ 3600
        mMinutes = (mSeconds - (mHours * 3600)) \ 60
        mSeconds = mSeconds - ((mHours * 3600) + (mMinutes * 60))

        Dim sSeconds As String = mSeconds
        Dim sMinutes As String = mMinutes
        Dim sHours As String = mHours

        If mSeconds < 10 Then sSeconds = "0" + sSeconds
        If mMinutes < 10 Then sMinutes = "0" + sMinutes
        If mHours < 10 Then sHours = "0" + sHours

        Return sHours & ":" & sMinutes & ":" & sSeconds

    End Function

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If (sniffing) Then
            StopSniffing()

        Else
            StartSniffing()

        End If
    End Sub

    Public Sub StartSniffing()
        If (CheckEmailEmpty()) Then
            StopSniffing()
            Return
        End If

        'SetWebConnexion()

        Panel2.Enabled = False
        sniffing = True
        Button2.Text = "Stop"

        timeSniffing = 0
        LastSniffed = -(sniffInterval - 1)

        wordsToFindtxt.ReadOnly = True
        TextBox1.ReadOnly = True
        urlBox1.ReadOnly = True

        If TextBox4.Text <> "************" And TextBox4.Text <> "" Then
            passwordGmailSave = TextBox4.Text
            TextBox4.Text = "************"
        End If


    End Sub
    Public Sub StopSniffing()
        Panel2.Enabled = True
        sniffing = False
        Button2.Text = "Start"

        wordsToFindtxt.ReadOnly = False
        urlBox1.ReadOnly = False
        TextBox1.ReadOnly = False

    End Sub

    Public Function CheckEmailEmpty() As Boolean

        If CheckBox1.Checked And (TextBox2.Text = "" Or TextBox2.Text = "" Or TextBox2.Text = "") Then
            MsgBox("Email alert activated but email parameters not completed. Sniff will not start")
            Return True
        End If

        Return False

    End Function

    Public Sub SendEmail()

        Try
            Dim fromAux As String = TextBox2.Text
            Dim netEmail As New System.Net.Mail.MailMessage

            Dim smtpEmail As New System.Net.Mail.SmtpClient

            smtpEmail.Host = "smtp.gmail.com"
            smtpEmail.Port = CInt(587)
            smtpEmail.DeliveryMethod = Net.Mail.SmtpDeliveryMethod.Network
            'smtpEmail.UseDefaultCredentials = bAutenticacionServidorSMTP
            smtpEmail.Credentials = New System.Net.NetworkCredential(fromAux.Trim, passwordGmailSave)
            smtpEmail.EnableSsl = True
            netEmail.IsBodyHtml = True
            netEmail.Priority = Net.Mail.MailPriority.Normal
            netEmail.From = New System.Net.Mail.MailAddress(fromAux)

            Dim assumpteCorreu As String = TextBox3.Text
            If assumpteCorreu = "" Then
                assumpteCorreu = "Tierra a la vista!"
            End If

            Dim cosCorreu As String = "S'ha trobat les següents keywords en la pàgina: " + urlBox1.Text + newLine + "Hora: " + Now.ToString
            Dim keywordsAux As String() = objectsFound.Split(",")
            For Each keyword In keywordsAux
                If keyword <> "" Then
                    cosCorreu += newLine + "-> " + keyword
                End If
            Next

            netEmail.Subject = assumpteCorreu

            Dim htmlView As Net.Mail.AlternateView = Net.Mail.AlternateView.CreateAlternateViewFromString(cosCorreu)
            netEmail.AlternateViews.Add(htmlView)

            netEmail.To.Clear()
            netEmail.To.Add(fromAux)


            smtpEmail.Send(netEmail)
        Catch ex As Exception
            StopSniffing()
            MsgBox("EMAIL ERROR:" + ex.ToString + newLine + "IMPORTANT: Es possible que falti activar els permisos de gmail, apretar botó 'Permission'")
        End Try

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        'boto de goto web
        Process.Start(urlBox1.Text)
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        'link de google que s'ha d'acceptar per poder rebre emails
        Process.Start("https://myaccount.google.com/u/0/lesssecureapps?pli=1")
    End Sub

    Private Sub saveData()
        SaveSetting(My.Application.Info.ProductName, "Settings", "email", TextBox2.Text)
        If TextBox4.Text = "************" Then
            SaveSetting(My.Application.Info.ProductName, "Settings", "password", passwordGmailSave)
        Else
            SaveSetting(My.Application.Info.ProductName, "Settings", "password", TextBox4.Text)
        End If
        SaveSetting(My.Application.Info.ProductName, "Settings", "assumpte", TextBox3.Text)
    End Sub

    Private Sub loadData()
        TextBox2.Text = GetSetting(My.Application.Info.ProductName, "Settings", "email")
        passwordGmailSave = GetSetting(My.Application.Info.ProductName, "Settings", "password")
        TextBox4.Text = "************"
        TextBox3.Text = GetSetting(My.Application.Info.ProductName, "Settings", "assumpte")
        CheckBox1.Checked = True
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        saveData()
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        loadData()
    End Sub
End Class
