<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    Home Page
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>NuGet Sample Feed</h2>
    <p>
        This is a sample implementation of a NuGet gallery. Click on the OData tab to see the list of packages.
    </p>
</asp:Content>
