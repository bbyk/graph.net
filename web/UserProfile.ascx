<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="UserProfile.ascx.cs" Inherits="FacebookAPI.WebUI.UserProfile" %>

<asp:panel runat="server" groupingtext="Current session">
  UserId: <asp:literal id="uiUserId" runat="server" /><br />
  UserName: <asp:literal id="uiUserName" runat="server" /><br />
  Birthday: <asp:literal id="uiBirthday" runat="server" /><br />
  Gender: <asp:literal id="uiGender" runat="server" />
</asp:panel>

<asp:panel runat="server" groupingtext="AppWide session">
  UserId: <asp:literal id="uiAppUserId" runat="server" /><br />
  UserName: <asp:literal id="uiAppUserName" runat="server" /><br />
  Gender: <asp:literal id="uiAppGender" runat="server" />
</asp:panel>

[<asp:linkbutton runat="server" onclick="OnLogout">logout</asp:linkbutton>]