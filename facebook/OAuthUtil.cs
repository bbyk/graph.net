#region Boris Byk's boilerplate notice
/*
 * Copyright 2010 Boris Byk.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may
 * not use this file except in compliance with the License. You may obtain
 * a copy of the License at
 *
 *  http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations
 * under the License.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using System.Web.SessionState;

namespace Facebook
{
    /// <summary>
    /// </summary>
    public class OAuthContext : IAuthContext
    {
        #region Statics and contants
        /// <summary>
        /// </summary>
        public static readonly Dictionary<string, string> EmptyParams = new Dictionary<string, string>();
        #endregion

        #region Members
        readonly string _appId;
        readonly string _appSecret;
        CultureInfo _ci;
        FacebookApi _api, _appWideApi;
        Session _fbSession;
        string _sessionStoreKey;
        #endregion

        #region Contructor

        /// <summary>
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="appSecret"></param>
        public OAuthContext([NotNull] string appId, [NotNull] string appSecret)
        {
            if (String.IsNullOrEmpty(appId))
                throw FacebookApi.Nre("appId");
            if (String.IsNullOrEmpty(appSecret))
                throw FacebookApi.Nre("appSecret");

            _appId = appId;
            _appSecret = appSecret;
        }

        #endregion

        #region Public methods and properties

        ///<summary>
        ///</summary>
        public string AppId
        {
            get { return _appId; }
        }

        ///<summary>
        ///</summary>
        public string AppSecret
        {
            get { return _appSecret; }
        }

        ///<summary>
        ///</summary>
        public FacebookApi ApiClient
        {
            get { return _api ?? (_api = new FacebookApi(AccessToken, Culture)); }
        }

        ///<summary>
        ///</summary>
        public FacebookApi AppApiClient
        {
            get { return _appWideApi ?? (_appWideApi = new FacebookApi(AppAccessToken, Culture)); }
        }

        ///<summary>
        ///</summary>
        ///<exception cref="FacebookApiException"></exception>
        public string AccessToken
        {
            get
            {
                if (_fbSession == null)
                    throw new FacebookApiException("Connect", "Session is not available");

                if (_fbSession.IsExpired)
                    throw new FacebookApiException("Connect", "Token is expired");

                return _fbSession.OAuthToken;
            }
        }

        /// <summary>
        /// </summary>
        public DateTime Expires
        {
            get { return _fbSession == null ? default(DateTime) : _fbSession.Expires; }
        }

        ///<summary>
        ///</summary>
        public string AppAccessToken
        {
            get { return AppId + "|" + _appSecret; }
        }

        ///<summary>
        ///</summary>
        public string SessionStoreKey
        {
            get { return _sessionStoreKey ?? (_sessionStoreKey = "fbs_" + AppId); }
        }

        ///<summary>
        ///</summary>
        public CultureInfo Culture
        {
            get { return _ci ?? CultureInfo.CurrentCulture; }
            set { _ci = value; }
        }

        /// <summary>
        /// </summary>
        public bool IsAuthenticated { get { return _fbSession != null && !_fbSession.IsExpired; } }

        ///<summary>
        ///</summary>
        ///<param name="nextUrl"></param>
        ///<returns></returns>
        public string GetLoginUrl([NotNull] Uri nextUrl)
        {
            return GetLoginUrl(nextUrl, EmptyParams);
        }

        ///<summary>
        ///</summary>
        ///<param name="nextUrl"></param>
        ///<param name="params"></param>
        ///<returns></returns>
        ///<exception cref="ArgumentNullException"></exception>
        public string GetLoginUrl([NotNull] Uri nextUrl, Dictionary<string, string> @params)
        {
            if (nextUrl == null)
                throw FacebookApi.Nre("nextUrl");

            var p = new Dictionary<string, string>
            {
                {"client_id", AppId},
                {"redirect_uri", CanvasAuthContext.StripAwayProhibitedKeys(nextUrl)},
            };

            // merge params
            if (@params != null)
                foreach (var kv in @params)
                    p[kv.Key] = kv.Value;

            return "https://graph.facebook.com/oauth/authorize?" + FacebookApi.EncodeDictionary(p);
        }

        ///<summary>
        ///</summary>
        ///<param name="nextUrl"></param>
        ///<returns></returns>
        public string GetLogoutUrl(Uri nextUrl)
        {
            return GetLogoutUrl(nextUrl, EmptyParams);
        }

        /// <summary>
        /// </summary>
        /// <param name="nextUrl"></param>
        /// <param name="params"></param>
        /// <returns></returns>
        public string GetLogoutUrl(Uri nextUrl, Dictionary<string, string> @params)
        {
            if (nextUrl == null)
                throw FacebookApi.Nre("nextUrl");

            string next = CanvasAuthContext.StripAwayProhibitedKeys(nextUrl);

            var p = new Dictionary<string, string> {
                                                       { "next" , next },
                                                       { "access_token" , AccessToken }};

            foreach (var kv in @params ?? EmptyParams)
                p[kv.Key] = kv.Value;

            return "https://www.facebook.com/logout.php?" + FacebookApi.EncodeDictionary(p);
        }

        ///<summary>
        ///</summary>
        ///<param name="context"></param>
        ///<returns></returns>
        ///<exception cref="ArgumentNullException"></exception>
        public bool AuthenticateRequest([NotNull] HttpContext context)
        {
            if (context == null)
                throw FacebookApi.Nre("context");

            string code = context.Request.QueryString["code"];
            if (!String.IsNullOrEmpty(code))
            {
                Authenticate(code, GetCurrentUrl(context));
                SaveSession(context);
            }
            else
            {
                HttpSessionState httpSession = context.Session;
                if (httpSession != null)
                    _fbSession = httpSession[SessionStoreKey] as Session;
            }

            return _fbSession != null;
        }

        /// <summary>
        /// </summary>
        /// <exception cref="FacebookApiException"></exception>
        /// <exception cref="TimeoutException"></exception>
        public void Authenticate([NotNull] string code, [NotNull] string redirectUri)
        {
            if (String.IsNullOrEmpty(code))
                throw FacebookApi.Nre("code");
            if (String.IsNullOrEmpty(redirectUri))
                throw FacebookApi.Nre("redirectUri");

            string contentType;
            string json = new FacebookApi().Request(
                "https://graph.facebook.com/oauth/access_token",
                HttpVerb.Post,
                new Dictionary<string, string>
                {
                    {"client_id", _appId},
                    {"client_secret", _appSecret},
                    {"redirect_uri", redirectUri},
                    {"code", code}
                },
                out contentType);

            ParseAuthResult(contentType, json);
        }

        ///<summary>
        ///</summary>
        ///<param name="context"></param>
        ///<param name="cb"></param>
        ///<param name="state"></param>
        public IAsyncResult BeginAuthenticateRequest([NotNull] HttpContext context, AsyncCallback cb, object state)
        {
            if (context == null)
                throw FacebookApi.Nre("context");

            var tar = new TypedAsyncResult<bool>(cb, state);
            string code = context.Request.QueryString["code"];
            if (!String.IsNullOrEmpty(code))
                return BeginAuthenticate(code, GetCurrentUrl(context),
                    tar.AsSafe(ar =>
                    {
                        EndAuthenticate(ar);
                        SaveSession(context);

                        tar.Complete(IsAuthenticated, false);
                    }),
                    null);

            HttpSessionState httpSession = context.Session;
            if (httpSession != null)
                _fbSession = httpSession[SessionStoreKey] as Session;

            tar.Complete(true);

            return tar;
        }

        /// <summary>
        /// </summary>
        /// <param name="code"></param>
        /// <param name="redirectUri"></param>
        /// <param name="cb"></param>
        /// <param name="state"></param>
        /// <exception cref="FacebookApiException"></exception>
        public IAsyncResult BeginAuthenticate([NotNull] string code, [NotNull] string redirectUri, AsyncCallback cb, object state)
        {
            if (String.IsNullOrEmpty(redirectUri))
                throw new ArgumentNullException("redirectUri");
            if (String.IsNullOrEmpty(code))
                throw FacebookApi.Nre("code");

            return new FacebookApi().BeginRequest(
                "https://graph.facebook.com/oauth/access_token",
                HttpVerb.Post,
                new Dictionary<string, string>
                {
                    {"client_id", _appId},
                    {"client_secret", _appSecret},
                    {"redirect_uri", redirectUri},
                    {"code", code}
                },
                cb, state);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        /// <exception cref="FacebookApiException"></exception>
        public void EndAuthenticate(IAsyncResult ar)
        {
            var data = TypedAsyncResult<FacebookApi.ResponseData>.End(ar, FacebookApi.ExConverter);
            ParseAuthResult(data.ContentType, data.Json);
        }

        ///<summary>
        ///</summary>
        ///<param name="ar"></param>
        public bool EndAuthenticateRequest(IAsyncResult ar)
        {
            return TypedAsyncResult<bool>.End(ar, FacebookApi.ExConverter);
        }

        #endregion

        #region Private and protected methods

        long IAuthContext.UserId { get { throw new NotSupportedException(); }}

        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual string GetCurrentUrl(HttpContext context)
        {
            return context.Request.Url.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped);
        }

        ///<summary>
        ///</summary>
        ///<param name="context"></param>
        private void SaveSession(HttpContext context)
        {
            HttpSessionState httpSession = context.Session;
            if (httpSession != null)
                httpSession[SessionStoreKey] = _fbSession;
        }

        void ParseAuthResult(string contentType, string json)
        {
            switch (contentType)
            {
                case "text/plain":
                    NameValueCollection nvc = HttpUtility.ParseQueryString(json);
                    _fbSession = new Session
                    {
                        OAuthToken = nvc["access_token"],
                        Expires = DateTime.UtcNow.AddSeconds(Convert.ToInt64(nvc["expires"], CultureInfo.InvariantCulture)),
                    };
                    break;
                case "text/javascript":
                    var obj = JsonObject.CreateFromString(json, CultureInfo.InvariantCulture);
                    if (obj.IsDictionary && obj.Dictionary.ContainsKey("error"))
                        throw FacebookApi.GraphError(obj);

                    throw FacebookApi.UnexpectedResponse(json);
                default:
                    throw FacebookApi.UnexpectedResponse(json);
            }
        }

        #endregion
    }
}
