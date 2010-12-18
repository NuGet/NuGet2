<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<$safeprojectname$.Models.ChangePasswordModel>" %>

<asp:Content ID="changePasswordTitle" ContentPlaceHolderID="TitleContent" runat="server">
    Change Password
</asp:Content>

<asp:Content ID="changePasswordContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Change Password</h2>
    <p>
        Use the form below to change your password. 
    </p>
    <p>
        New passwords are required to be a minimum of $if$ ($targetframeworkversion$ >= 4.0)<%: ViewData["PasswordLength"] %>$else$<%= Html.Encode(ViewData["PasswordLength"]) %>$endif$ characters in length.
    </p>

    <% using (Html.BeginForm()) { %>
        <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ValidationSummary(true, "Password change was unsuccessful. Please correct the errors and try again.") %>
        <div>
            <fieldset>
                <legend>Account Information</legend>
                
                <div class="editor-label">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.LabelFor(m => m.OldPassword) %>
                </div>
                <div class="editor-field">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.PasswordFor(m => m.OldPassword) %>
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ValidationMessageFor(m => m.OldPassword) %>
                </div>
                
                <div class="editor-label">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.LabelFor(m => m.NewPassword) %>
                </div>
                <div class="editor-field">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.PasswordFor(m => m.NewPassword) %>
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ValidationMessageFor(m => m.NewPassword) %>
                </div>
                
                <div class="editor-label">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.LabelFor(m => m.ConfirmPassword) %>
                </div>
                <div class="editor-field">
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.PasswordFor(m => m.ConfirmPassword) %>
                    <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ValidationMessageFor(m => m.ConfirmPassword) %>
                </div>
                
                <p>
                    <input type="submit" value="Change Password" />
                </p>
            </fieldset>
        </div>
    <% } %>
</asp:Content>
