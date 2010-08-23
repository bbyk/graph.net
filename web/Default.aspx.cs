using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Facebook;

namespace FacebookAPI.WebUI
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var identity = (Facebook.Identity)Context.User.Identity;
            JSONObject data = identity.Canvas.ApiClient.Get("/me");
            uiUserId.Text = identity.Canvas.UserId.ToString();
            uiUserName.Text = data.Dictionary["name"].String;
        }
    }
}
