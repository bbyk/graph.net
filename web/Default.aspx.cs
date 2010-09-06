using System;
using System.Web.UI;
using Facebook;

namespace FacebookAPI.WebUI
{
    public partial class Default : Page
    {
        protected void OnCanvasLogin(object sender, EventArgs args)
        {
            Response.Redirect("~/Canvas/Default.aspx");
        }

        protected void OnConnectLogin(object sender, EventArgs args)
        {
            Response.Redirect("~/Connect/Default.aspx");
        }
    }
}
