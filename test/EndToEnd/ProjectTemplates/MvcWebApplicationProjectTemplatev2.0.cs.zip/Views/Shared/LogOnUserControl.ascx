<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%
    if (Request.IsAuthenticated) {
%>
        Welcome <b>$if$ ($targetframeworkversion$ >= 4.0)<%: Page.User.Identity.Name %>$else$<%= Html.Encode(Page.User.Identity.Name) %>$endif$</b>!
        [ <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ActionLink("Log Off", "LogOff", "Account") %> ]
<%
    }
    else {
%> 
        [ <%$if$ ($targetframeworkversion$ >= 4.0):$else$=$endif$ Html.ActionLink("Log On", "LogOn", "Account") %> ]
<%
    }
%>
