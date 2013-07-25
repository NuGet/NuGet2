<%@ Page Language="VB" %>

<script runat="server">
    Protected Overrides Sub OnLoad(e As EventArgs)
        System.Web.Security.FormsAuthentication.SignOut()
        
        Response.Clear()
        Response.Redirect(Request.UrlReferrer.ToString())
    End Sub
</script>

<!DOCTYPE HTML>
<html>
<head>
    <title>Log out</title>
</head>
<body>
</body>
</html>
