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

            IAsyncResult rar = null;
            AddOnPreRenderCompleteAsync(
                (s, args, cb, state) => identity.Canvas.AppApiClient.BeginGet("/" + identity.Canvas.UserId, cb, state),
                ar => rar = ar);

            PreRenderComplete += delegate
            {
                // call 'end' method here, because we want to let ASP.NET catch possible exceptions.
                data = identity.Canvas.AppApiClient.EndGet(rar);
                uiAppUserId.Text = data.Dictionary["id"].Integer.ToString();
                uiAppUserName.Text = data.Dictionary["name"].String;
                uiAppGender.Text = data.Dictionary["gender"].String;
            };
        }
    }
}
