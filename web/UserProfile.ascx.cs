using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using Facebook;

namespace FacebookAPI.WebUI
{
    public partial class UserProfile : UserControl
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            var identity = (Identity)Context.User.Identity;
            JsonObject data = identity.AuthContext.ApiClient.Get("me");
            uiUserId.Text = data.Dictionary["id"].String;
            uiUserName.Text = data.Dictionary["name"].String;
            uiBirthday.Text = data.Dictionary["birthday"].DateTime.ToLongDateString();
            uiGender.Text = data.Dictionary["gender"].String;
            
            var syncData = identity.AuthContext.ApiClient.Get("method/friends.getAppUsers");
            data = identity.AuthContext.ApiClient.EndGet(identity.AuthContext.ApiClient.BeginGet("method/friends.getAppUsers", null, null));
            if (!syncData.IsArray || !data.IsArray || syncData.Array.Length != data.Array.Length)
                throw new ApplicationException();

            var ids = data.Array.Select(j => j.Integer.ToString()).ToArray();

            if (!data.IsArray)
                uiAppFriends.Visible = false;
            else
            {
                data = identity.AuthContext.ApiClient.Get(null, new Dictionary<string, string>() { { "ids", String.Join(",", ids) } });
                if (data.IsDictionary)
                    uiAppFriends.Text = String.Join(", ", ids.Select(id => data.Dictionary[id.ToString()].Dictionary["name"].String).ToArray());
            }
            IAsyncResult rar = null;
            Page.AddOnPreRenderCompleteAsync(
                (s, args, cb, state) => identity.AuthContext.AppApiClient.BeginGet(uiUserId.Text, cb, state),
                ar => rar = ar);

            Page.PreRenderComplete += delegate
            {
                // call 'end' method here, because we want to let ASP.NET catch possible exceptions.
                data = identity.AuthContext.AppApiClient.EndGet(rar);
                uiAppUserId.Text = data.Dictionary["id"].Integer.ToString();
                uiAppUserName.Text = data.Dictionary["name"].String;
                uiAppGender.Text = data.Dictionary["gender"].String;
            };
        }

        protected void OnLogout(object sender, EventArgs args)
        {
            var identity = (Identity)Context.User.Identity;
            Context.Session.Abandon();
            Response.Redirect(identity.AuthContext.GetLogoutUrl(new Uri("http://localhost/graph.net"), null));
        }
    }
}