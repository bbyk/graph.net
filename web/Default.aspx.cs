using System;
using System.Web.UI;
using Facebook;

namespace FacebookAPI.WebUI
{
    public partial class Default : Page
    {
        protected override void OnInit(EventArgs e)
        {
            var identity = (Identity)Context.User.Identity;
            JsonObject data = identity.Canvas.ApiClient.Get("/me");
            uiUserId.Text = identity.Canvas.UserId.ToString();
            uiUserName.Text = data.Dictionary["name"].String;
            uiBirthday.Text = data.Dictionary["birthday"].DateTime.ToLongDateString();
            uiGender.Text = data.Dictionary["gender"].String;
        }
    }
}
