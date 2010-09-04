<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="FacebookAPI.WebUI.Default" async="true" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
</head>
<body>
    <form id="frm" runat="server">

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

    </form>
</body>
</html>
