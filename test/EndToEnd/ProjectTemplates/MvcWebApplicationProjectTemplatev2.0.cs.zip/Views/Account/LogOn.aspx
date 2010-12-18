<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<$safeprojectname$.Models.LogOnModel>" %>

<asp:Content ID="loginTitle" ContentPlaceHolderID="TitleContent" runat="server">
    Log On
</asp:Content>

<asp:Content ID="loginContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Log On</h2>
    <p>
        Please enter your username and password. <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ActionLink("Register", "Register") %> if you don't have an account.
    </p>

    <% using (Html.BeginForm()) { %>
        <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ValidationSummary(true, "Login was unsuccessful. Please correct the errors and try again.") %>
        <div>
            <fieldset>
                <legend>Account Information</legend>
                
                <div class="editor-label">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.LabelFor(m => m.UserName) %>
                </div>
                <div class="editor-field">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.TextBoxFor(m => m.UserName) %>
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ValidationMessageFor(m => m.UserName) %>
                </div>
                
                <div class="editor-label">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.LabelFor(m => m.Password) %>
                </div>
                <div class="editor-field">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.PasswordFor(m => m.Password) %>
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ValidationMessageFor(m => m.Password) %>
                </div>
                
                <div class="editor-label">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.CheckBoxFor(m => m.RememberMe) %>
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.LabelFor(m => m.RememberMe) %>
                </div>
                
                <p>
                    <input type="submit" value="Log On" />
                </p>
            </fieldset>
        </div>
    <% } %>
</asp:Content>
