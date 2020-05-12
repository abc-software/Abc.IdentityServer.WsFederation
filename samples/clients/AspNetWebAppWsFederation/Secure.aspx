<%@ Page Title="Secure" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Secure.aspx.cs" Inherits="AspNetWebAppWsFederation.Secure" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
<h2>Claims</h2>

<dl>
    <% foreach (var claim in (User.Identity as System.Security.Claims.ClaimsIdentity).Claims) 
    {%>
        <dt><%: claim.Type %></dt>
        <dd><%: claim.Value %></dd>
    <%}%>
</dl>

</asp:Content>
