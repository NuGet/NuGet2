<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<$safeprojectname$.Models.RegisterModel>" %>

<asp:Content ID="registerTitle" ContentPlaceHolderID="TitleContent" runat="server">
    Register
</asp:Content>

<asp:Content ID="registerContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Create a New Account</h2>
    <p>
        Use the form below to create a new account. 
    </p>
    <p>
        Passwords are required to be a minimum of $if$ ($targetframeworkversion$ >= 4.0)<%: ViewData["PasswordLength"] %>$else$<%= Html.Encode(ViewData["PasswordLength"]) %>$endif$ characters in length.
    </p>

    <% using (Html.BeginForm()) { %>
        <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ValidationSummary(true, "Account creation was unsuccessful. Please correct the errors and try again.") %>
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
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.LabelFor(m => m.Email) %>
                </div>
                <div class="editor-field">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.TextBoxFor(m => m.Email) %>
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ValidationMessageFor(m => m.Email) %>
                </div>
                
                <div class="editor-label">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.LabelFor(m => m.Password) %>
                </div>
                <div class="editor-field">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.PasswordFor(m => m.Password) %>
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ValidationMessageFor(m => m.Password) %>
                </div>
                
                <div class="editor-label">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.LabelFor(m => m.ConfirmPassword) %>
                </div>
                <div class="editor-field">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.PasswordFor(m => m.ConfirmPassword) %>
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ValidationMessageFor(m => m.ConfirmPassword) %>
                </div>
                
                <p>
                    <input type="submit" value="Register" />
                </p>
            </fieldset>
        </div>
    <% } %>
</asp:Content>
