using System;
using System.Collections.Generic;
using System.Security.Principal;
using Facebook;
using System.Web;
using System.Globalization;

namespace FacebookAPI.WebUI
{
    public class AuthenticationModule : IApplicationBindings, IHttpModule
    {
        #region IHttpModule Members

        public void Dispose()
        {
        }

        public void Init(HttpApplication app)
        {
            // a fix for IE. it wants us to declare P3P header (see http://msdn.microsoft.com/en-us/library/ms537343.aspx) when under iframe.
            app.PreSendRequestHeaders += (s, e) => app.Response.AddHeader("P3P", "CP=\"CAO PSA OUR\"");
            app.PostAcquireRequestState += (s, e) => OnEnter(app.Context);
        }

        #endregion

        void OnEnter(HttpContext context)
        {
            if (context.Session == null)
                return;

            // the following browsers has 'Accept cookie from site I visit settings'. we need to make them store cookie.
            HttpBrowserCapabilities br = context.Request.Browser;
            bool forceLogin = br.IsBrowser("opera") || (br.IsBrowser("safari") && !br.IsBrowser("googlechrome"));

            forceLogin = forceLogin && context.Session["after_login"] == null;

            var util = new CanvasUtil(this, CultureInfo.CurrentCulture);

            if (util.Authenticate(context) && !forceLogin)
            {
                context.User = new GenericPrincipal(new Identity(util), null);
                var step = context.Session["after_login"] as int?;

                if (!step.HasValue || step.Value != 0) return;

                context.Session["after_login"] = 1;
                CanvasUtil.RedirectFromIFrame(context, util.ResolveCanvasPageUrl("~/"));
            }
            else
            {
                context.Session["after_login"] = 0;
                var @params = new Dictionary<string, string> {{"req_perms", "user_birthday"}, {"cancel_url", "http://www.facebook.com"}};
                CanvasUtil.RedirectFromIFrame(context, util.GetLoginUrl(context.Request.Url, @params));
                return;
            }
        }

        #region IApplicationBindings Members

        public string AppId
        {
            get { return "119774131404605"; }
        }

        public string AppSecret
        {
            get { return "9f4233f1193a1affd3de35a305c06a0c"; }
        }

        public Uri SiteUrl
        {
            get { return new Uri("http://localhost:24526/"); }
        }

        public Uri CanvasPage
        {
            get { return new Uri("http://apps.facebook.com/graphdotnet/"); }
        }

        #endregion
    }
}
