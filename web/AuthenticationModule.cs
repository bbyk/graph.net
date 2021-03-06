﻿using System;
using System.Collections.Generic;
using System.Security.Principal;
using Facebook;
using System.Web;
using System.Globalization;
using System.Diagnostics;

namespace FacebookAPI.WebUI
{
    public abstract class BaseAuthModule
    {
        public void Dispose()
        {
        }

        public string AppId
        {
            get { return "119774131404605"; }
        }

        public string AppSecret
        {
            get { return "9f4233f1193a1affd3de35a305c06a0c"; }
        }
    }

    public class OAuthAuthenticationModule : BaseAuthModule, IHttpModule
    {
        public void Init(HttpApplication app)
        {
            app.AddOnPostAcquireRequestStateAsync((s, e, cb, state) =>
            {
                HttpContext context = app.Context;

                var tar = new TypedAsyncResult<Identity>(cb, state);
                if (context.Session == null || !context.Request.Url.AbsolutePath.Contains("/Connect"))
                {
                    tar.Complete(true);
                    return tar;
                }

                var util = new OAuthContext(AppId, AppSecret)
                {
                    Culture = CultureInfo.CurrentCulture,
                    ExProcessor = ex => Debug.Write(ex),
                };

                util.SessionStorage = new AspNetSessionStore(context, util);

                util.BeginAuthenticateRequest(context, tar.AsSafe(ar =>
                {
                    util.EndAuthenticateRequest(ar);
                    tar.Complete(new Identity(util), false);
                }), null);
                return tar;
            },
            ar =>
            {
                var ident = TypedAsyncResult<Identity>.End(ar, null);
                if (ident == null)
                    return;

                HttpContext context = app.Context;
                if (!ident.IsAuthenticated)
                {
                    //var @params = new Dictionary<string, string> { { "scope", "user_birthday" } };
                    context.Response.Redirect(ident.AuthContext.GetLoginUrl(context.Request.Url, new LoginParams { ReqPerms = "user_birthday" }), false);
                    context.ApplicationInstance.CompleteRequest();
                    return;
                }

                context.User = new GenericPrincipal(ident, null);
            });
        }
    }

    public class CanvasAuthenticationModule : BaseAuthModule, IApplicationBindings, IHttpModule
    {
        public void Init(HttpApplication app)
        {
            // a fix for IE. it wants us to declare P3P header (see http://msdn.microsoft.com/en-us/library/ms537343.aspx) when under iframe.
            app.PreSendRequestHeaders += (s, e) => app.Response.AddHeader("P3P", "CP=\"CAO PSA OUR\"");
            app.PostAcquireRequestState += (s, e) => OnEnter(app.Context);
        }

        void OnEnter(HttpContext context)
        {
            if (context.Session == null || !context.Request.Url.AbsolutePath.Contains("/Canvas"))
                return;

            // the following browsers has 'Accept cookie from site I visit settings'. we need to make them store cookie.
            HttpBrowserCapabilities br = context.Request.Browser;
            bool forceLogin = br.IsBrowser("opera") || (br.IsBrowser("safari") && !br.IsBrowser("googlechrome"));

            forceLogin = forceLogin && context.Session["after_login"] == null;

            var util = new CanvasAuthContext(this)
            {
                Culture = CultureInfo.CurrentCulture,
                ExProcessor = ex => Debug.Write(ex),
            };

            util.SessionStorage = new CookieSessionStore(context, util);

            if (util.Authenticate(context) && !forceLogin)
            {
                context.User = new GenericPrincipal(new Identity(util), null);
                var step = context.Session["after_login"] as int?;

                if (!step.HasValue || step.Value != 0) return;

                context.Session["after_login"] = 1;
                CanvasAuthContext.RedirectFromIFrame(context, util.ResolveCanvasPageUrl(context.Request.AppRelativeCurrentExecutionFilePath));
            }
            else if (!(context.Handler is IClientAuth))
            {
                context.Session["after_login"] = 0;
                var @params = new Dictionary<string, string> { { "req_perms", "user_birthday" } };
                CanvasAuthContext.RedirectFromIFrame(context, util.GetLoginUrl(context.Request.Url, @params));
                return;
            }
        }

        #region IApplicationBindings Members

        public string SiteUrl
        {
            get { return "http://localhost/graph.net/"; }
        }

        public string CanvasPage
        {
            get { return "http://apps.facebook.com/graphdotnet/"; }
        }

        #endregion
    }

    /// <summary>
    /// Just a marker to indicate we allow to login on the page by means of js.
    /// </summary>
    internal interface IClientAuth
    {
    }
}
