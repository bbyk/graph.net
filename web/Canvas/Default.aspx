<%@ Page Language="C#" AutoEventWireup="true" async="true" %>
<%@ register src="~/UserProfile.ascx" tagprefix="fb" tagname="profile" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
</head>
<body>
  <form id="frm" runat="server">
    <fb:profile runat="server" />
  </form>
</body>
</html>
