using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Facebook;

namespace FacebookAPI.WebUI.Canvas
{
    public partial class Login : System.Web.UI.Page, IClientAuth
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Context.User == null || !Context.User.Identity.IsAuthenticated)
                return;

            var identity = (Identity)Context.User.Identity;
            JsonObject data = identity.AuthContext.ApiClient.Get("me");
            uiName.Text = data.IsDictionary && data.Dictionary.ContainsKey("name") ? data.Dictionary["name"].String : "N/A";
        }
    }
}
