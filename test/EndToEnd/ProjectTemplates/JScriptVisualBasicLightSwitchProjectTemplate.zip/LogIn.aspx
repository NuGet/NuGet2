<%@ Page Inherits="Microsoft.LightSwitch.Security.ServerGenerated.Implementation.LogInPageBase" %>

<!DOCTYPE HTML>
<html>
<head>
    <meta name="HandheldFriendly" content="true" />
    <meta name="viewport" content="width=device-width, initial-scale=1, minimum-scale=1, maximum-scale=1, user-scalable=no" />
    <title>Log In</title>
    <style type="text/css">
        /* Here you can customize your login screen */
        html {
            background: #ffffff;
        }

        html,
        body,
        .labelStyle {
            color: #444444;
        }

        h1 {
            color: #444444;
        }

        .requiredStyle {
            color: #FF1B1B;
        }

        input.buttonStyle {
            color: #444444;
            background-color: #f1f1f1;
            border: 1px solid #ababab;
        }

            input.buttonStyle:hover {
                background-color: #dfebf4;
            }

            input.buttonStyle:active {
                background-color: #e3e3e3;
            }

        .textBoxStyle {
            color: #444444;
            background-color: #fcfcfc;
            border: 1px solid #ababab;
        }

        .failureNotification {
            color: #ff0000;
        }

        /* login layout styling */
        * {
            margin: 0px;
        }

        html {
            height: 100%;
            width: 100%;
        }

        html, body {
            font-family: 'Segoe UI','Frutiger','Helvetica Neue',Helvetica,Arial,sans-serif;
            font-size: 16px;
            font-weight: normal;
        }

        h1 {
            font-family: 'Segoe Light','Segoe UI Light','Frutiger','Helvetica Neue',Helvetica,Arial,sans-serif;
            font-size: 40px;
            text-align: left;
            letter-spacing: -1pt;
            font-weight: normal!important;
            margin-bottom: 12px;
        }

        .accountInfo {
            width: 95%;
            max-width: 310px;
            position: absolute;
            top: 50%;
            margin-top: -144px;
            left: 50%;
            margin-left: -155px;
        }

        .labelStyle {
            font-family: 'Segoe UI Semibold', 'Frutiger','Helvetica Neue Semibold',Helvetica,Arial,sans-serif;
            font-weight: 700;
        }

        .requiredStyle {
            font-size: 24px;
            line-height: 14px;
            height: 12px;
            vertical-align: bottom;
            margin-left: 5px;
        }

        input.buttonStyle {
            font-family: 'Segoe UI','Frutiger','Helvetica Neue',Helvetica,Arial,sans-serif;
            padding: 5px 10px;
            font-weight: bold;
            border-radius: 0px;
            font-size: 16px;
            cursor: pointer;
            -webkit-appearance: none;
        }

        .textBoxStyle {
            background-image: none;
            font-size: 16px;
            display: block;
            outline: 0;
            height: 36px;
            padding: 1px 8px;
            margin: 0px;
            width: 100%;
            max-width: 292px;
            line-height: 36px;
        }

        .submit-login {
            margin-top: 10px;
        }

        .rememberme {
            margin-bottom: 10px;
        }

        input[type=checkbox] {
            margin: 0px 6px 0px 0px;
            vertical-align: -1px;
            cursor: pointer;
        }

        .checkStyle label {
            font-size: 15px;
        }
    </style>
</head>
<body>
    <form id="LogInForm" runat="server">
        <asp:Login ID="LoginUser" runat="server" EnableViewState="false" RenderOuterTable="false">
            <LayoutTemplate>
                <div class="accountInfo">
                    <h1>Log In</h1>
                    <div style="margin-bottom: 10px;">
                        <asp:Label ID="UsernameLabel" runat="server" AssociatedControlID="Username" Text="Username:" CssClass="labelStyle" />
                        <asp:RequiredFieldValidator ID="UsernameRequired" runat="server" ControlToValidate="Username" ValidationGroup="LoginUserValidationGroup" Text="*" ToolTip="Username is required" CssClass="requiredStyle" />
                        <asp:TextBox ID="Username" runat="server" CssClass="textBoxStyle" />
                    </div>
                    <div style="margin-bottom: 10px;">
                        <asp:Label ID="PasswordLabel" runat="server" AssociatedControlID="Password" Text="Password:" CssClass="labelStyle" />
                        <asp:RequiredFieldValidator ID="PasswordRequired" runat="server" ControlToValidate="Password" ValidationGroup="LoginUserValidationGroup" Text="*" ToolTip="Password is required" CssClass="requiredStyle" />
                        <asp:TextBox ID="Password" runat="server" TextMode="Password" CssClass="textBoxStyle" />
                    </div>
                    <div class="submit-login">
                        <div class="rememberme">
                            <asp:CheckBox ID="RememberMe" runat="server" Text="Remember me next time." CssClass="checkStyle" />
                        </div>
                        <div style="margin-bottom: 10px;" class="logInBtn">
                            <asp:Button ID="LoginButton" runat="server" CommandName="Login" ValidationGroup="LoginUserValidationGroup" Text="LOG IN" Width="112" Height="38" CssClass="buttonStyle" />
                        </div>
                    </div>
                    <span class="failureNotification">
                        <asp:Literal ID="FailureText" runat="server" />
                    </span>
                    <asp:ValidationSummary ID="LoginUserValidationSummary" runat="server" ValidationGroup="LoginUserValidationGroup" />
                </div>
            </LayoutTemplate>
        </asp:Login>
    </form>
</body>
</html>
