<%@ page language="C#" autoeventwireup="true" codebehind="Login.aspx.cs" inherits="FacebookAPI.WebUI.Canvas.Login" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title></title>
</head>
<body>
  <form id="frm" runat="server">
  <div>
    <div id="fb-root">
    </div>

    <script>
      window.fbAsyncInit = function() {
        FB.init({
          appId: '119774131404605',
          status: true,
          cookie: true,
          xfbml: true
        });

        FB.Event.subscribe('auth.login', function() {
          window.location.reload();
        });
      };
      (function() {
        var e = document.createElement('script'); e.async = true;
        e.src = document.location.protocol +
      '//connect.facebook.net/en_US/all.js';
        document.getElementById('fb-root').appendChild(e);
      } ());
    </script>

    <div>
      <p>Here is the example on how to use js/cookie-base authentication:</p>
      <p>Log in using JavaScript &amp; XFBML: <fb:login-button autologoutlink="true" perms="read_stream"></fb:login-button></p>
      <p>Your name is: <asp:literal id="uiName" runat="server" /></p>
    </div>
  </div>
  </form>
</body>
</html>
