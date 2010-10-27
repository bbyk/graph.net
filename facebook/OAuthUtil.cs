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

namespace Facebook
{
    /// <summary>
    /// Exposes site based authentication model. Fits well for a connected site.
    /// </summary>
    public class OAuthContext : AuthContextBase, IAuthContext
    {
        #region Statics and contants
        /// <summary>
        /// </summary>
        public static readonly Dictionary<string, string> EmptyParams = new Dictionary<string, string>();
        #endregion

        #region Members
        readonly string _appId;
        readonly string _appSecret;
        FacebookApi _api, _appWideApi;
        Session _fbSession;
        #endregion

        #region Contructor

        /// <summary>
        /// Initializes a new <see cref="OAuthContext"/> object with specified <paramref name="appId"/> and <paramref name="appSecret"/>.
        /// </summary>
        /// <param name="appId">Facebook application id.</param>
        /// <param name="appSecret">Application secret.</param>
        ///<exception cref="ArgumentNullException">either <paramref name="appId"/> or <paramref name="appSecret"/> is null.</exception>
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
        /// See <see cref="IAuthContext.AppId"/>.
        ///</summary>
        [NotNull]
        public string AppId
        {
            get { return _appId; }
        }

        ///<summary>
        /// See <see cref="IAuthContext.AppSecret"/>.
        ///</summary>
        [NotNull]
        public override string AppSecret
        {
            get { return _appSecret; }
        }

        /// <summary>
        /// See <see cref="IAuthContext.Session"/>.
        /// </summary>
        [CanBeNull]
        public Session Session { get { return _fbSession; } }

        ///<summary>
        /// See <see cref="IAuthContext.ApiClient"/>.
        ///</summary>
        [NotNull]
        public FacebookApi ApiClient
        {
            get
            {
                if (_api != null)
                    return _api;

                var api = CreateApiClient();
                api.AccessToken = AccessToken;

                return (_api = api);
            }
        }

        ///<summary>
        /// See <see cref="IAuthContext.AppApiClient"/>.
        ///</summary>
        [NotNull]
        public FacebookApi AppApiClient
        {
            get
            {
                if (_appWideApi != null)
                    return _appWideApi;

                var api = CreateApiClient();
                api.AccessToken = AppAccessToken;

                return (_appWideApi = api);
            }
        }

        ///<summary>
        /// See <see cref="IAuthContext.AccessToken"/>.
        ///</summary>
        ///<exception cref="FacebookApiException">Session is either null or expired.</exception>
        [NotNull]
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
        /// See <see cref="IAuthContext.Expires"/>.
        /// </summary>
        public DateTime Expires
        {
            get { return _fbSession == null ? default(DateTime) : _fbSession.Expires; }
        }

        ///<summary>
        /// See <see cref="IAuthContext.AppAccessToken"/>.
        ///</summary>
        [NotNull]
        public string AppAccessToken
        {
            get { return AppId + "|" + _appSecret; }
        }

        /// <summary>
        /// See <see cref="IAuthContext.IsAuthenticated"/>.
        /// </summary>
        public bool IsAuthenticated { get { return _fbSession != null && !_fbSession.IsExpired; } }

        ///<summary>
        /// See <see cref="IAuthContext.GetLoginUrl(Uri, Dictionary{String, String})"/>.
        ///</summary>
        ///<param name="nextUrl" />
        ///<returns />
        ///<exception cref="ArgumentNullException"><paramref name="nextUrl"/> is null.</exception>
        public string GetLoginUrl([NotNull] Uri nextUrl)
        {
            return GetLoginUrl(nextUrl, EmptyParams);
        }

        ///<summary>
        /// See <see cref="IAuthContext.GetLoginUrl(Uri, LoginParams)"/>.
        ///</summary>
        ///<param name="nextUrl" />
        ///<param name="params" />
        ///<returns />
        ///<exception cref="ArgumentNullException"><paramref name="nextUrl"/> is null.</exception>
        public string GetLoginUrl([NotNull] Uri nextUrl, [NotNull] LoginParams @params)
        {
            if (nextUrl == null)
                throw FacebookApi.Nre("nextUrl");

            var p = new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(@params.ReqPerms))
                p.Add("scope", @params.ReqPerms);
            if (@params.Display != LoginDialogDisplay.NotSet)
                p.Add("display", @params.Display.ToString().ToLowerInvariant());

            return GetLoginUrl(nextUrl, p);
        }

        ///<summary>
        /// See <see cref="IAuthContext.GetLoginUrl(Uri, Dictionary{String, String})"/>.
        ///</summary>
        ///<param name="nextUrl" />
        ///<param name="params" />
        ///<returns />
        ///<exception cref="ArgumentNullException"><paramref name="nextUrl"/> is null.</exception>
        public string GetLoginUrl([NotNull] Uri nextUrl, [CanBeNull] Dictionary<string, string> @params)
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
        /// See <see cref="IAuthContext.GetLogoutUrl(Uri, Dictionary{String, String})"/>.
        ///</summary>
        ///<param name="nextUrl" />
        ///<returns />
        ///<exception cref="ArgumentNullException"><paramref name="nextUrl"/> is null.</exception>
        public string GetLogoutUrl([NotNull] Uri nextUrl)
        {
            return GetLogoutUrl(nextUrl, EmptyParams);
        }

        ///<summary>
        /// See <see cref="IAuthContext.GetLogoutUrl(Uri, Dictionary{String, String})"/>.
        ///</summary>
        ///<param name="nextUrl" />
        ///<param name="params" />
        ///<returns />
        ///<exception cref="ArgumentNullException"><paramref name="nextUrl"/> is null.</exception>
        public string GetLogoutUrl([NotNull] Uri nextUrl, [CanBeNull] Dictionary<string, string> @params)
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
        /// Authenticates current request synchronously. Returns <c>true</c> if the request is authenticated and <see cref="Session"/> is set; otherwise <c>false</c>.
        ///</summary>
        ///<param name="context">http context to authenticate.</param>
        ///<returns></returns>
        ///<exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
        ///<exception cref="FacebookApiException"></exception>
        ///<exception cref="TimeoutException">The operation took longer then <see cref="IFacebookApiFactory.Timeout"/>.</exception>
        public bool AuthenticateRequest([NotNull] HttpContext context)
        {
            if (context == null)
                throw FacebookApi.Nre("context");

            bool saveSession = true;
            string code = context.Request.QueryString["code"];
            if (!String.IsNullOrEmpty(code))
            {
                Authenticate(code, GetCurrentUrl(context));
            }
            else
            {
                ISessionStorage ss = SessionStorage;
                if (ss != null)
                {
                    _fbSession = ss.Session;
                    if (_fbSession != null
                        && !ss.IsSecure
                        && _fbSession.Signature != GenerateSignature(_fbSession.ToJsonObject()))
                    {
                        _fbSession = null;
                    }

                    saveSession = _fbSession == null;
                }
            }

            if (saveSession)
                SaveSession(context);

            return _fbSession != null;
        }

        ///<summary>
        /// Authenticates with a <paramref name="code"/> given synchronously. See <see href="http://developers.facebook.com/docs/authentication/"/> for more information.
        ///</summary>
        ///<param name="redirectUri">a url which was passed to <see cref="GetLoginUrl(System.Uri)" /> as next url argument. It should not contain query part.</param>
        ///<param name="code">a verification string passed in the query string argument <c>code</c> when redirecting to <paramref name="redirectUri"/></param>
        ///<exception cref="FacebookApiException"></exception>
        ///<exception cref="TimeoutException">The operation took longer then <see cref="IFacebookApiFactory.Timeout"/>.</exception>
        ///<exception cref="ArgumentNullException">either <paramref name="code"/> or <paramref name="redirectUri"/> is null.</exception>
        public void Authenticate([NotNull] string code, [NotNull] string redirectUri)
        {
            if (String.IsNullOrEmpty(code))
                throw FacebookApi.Nre("code");
            if (String.IsNullOrEmpty(redirectUri))
                throw FacebookApi.Nre("redirectUri");

            string contentType;
            string json = CreateApiClient().Request(
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
        /// Begin to authenticate current request synchronously. Returns <c>true</c> if the request is authenticated and <see cref="Session"/> is set; otherwise <c>false</c>.
        ///</summary>
        ///<param name="context">http context to authenticate.</param>
        ///<param name="cb">a callback to call upon operation is completed.</param>
        ///<param name="state">the user state to pass to the callback.</param>
        ///<exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
        public IAsyncResult BeginAuthenticateRequest([NotNull] HttpContext context, [CanBeNull] AsyncCallback cb, [CanBeNull] object state)
        {
            if (context == null)
                throw FacebookApi.Nre("context");

            bool saveSession = true;
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

            ISessionStorage ss = SessionStorage;
            if (ss != null)
            {
                _fbSession = ss.Session;
                if (_fbSession != null
                    && !ss.IsSecure
                    && _fbSession.Signature != GenerateSignature(_fbSession.ToJsonObject()))
                {
                    _fbSession = null;
                }

                saveSession = _fbSession == null;
            }

            if (saveSession)
                SaveSession(context);

            tar.Complete(true);

            return tar;
        }

        ///<summary>
        /// Begins to authenticates with a <paramref name="code"/> given synchronously. See <see href="http://developers.facebook.com/docs/authentication/"/> for more information.
        ///</summary>
        ///<param name="redirectUri">a url which was passed to <see cref="GetLoginUrl(System.Uri)" /> as next url argument. It should not contain query part.</param>
        ///<param name="code">a verification string passed in the query string argument <c>code</c> when redirecting to <paramref name="redirectUri" /></param>
        ///<param name="cb">a callback to call upon operation is completed.</param>
        ///<param name="state">the user state to pass to the callback.</param>
        ///<exception cref="ArgumentNullException">either <paramref name="code"/> or <paramref name="redirectUri"/> is null.</exception>
        ///<returns>an async state to check completion with.</returns>
        public IAsyncResult BeginAuthenticate([NotNull] string code, [NotNull] string redirectUri, AsyncCallback cb, object state)
        {
            if (String.IsNullOrEmpty(redirectUri))
                throw new ArgumentNullException("redirectUri");
            if (String.IsNullOrEmpty(code))
                throw FacebookApi.Nre("code");

            return CreateApiClient().BeginRequest(
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

        ///<summary>
        /// Completes async authentication operation started by the <see cref="BeginAuthenticate"/> method.
        ///</summary>
        ///<param name="ar">async state of the operation.</param>
        ///<exception cref="ArgumentNullException"><paramref name="ar"/> is null.</exception>
        ///<exception cref="FacebookApiException"></exception>
        public void EndAuthenticate(IAsyncResult ar)
        {
            var data = TypedAsyncResult<FacebookApi.ResponseData>.End(ar, FacebookApi.ExConverter);
            ParseAuthResult(data.ContentType, data.Json);
        }

        ///<summary>
        /// Fetches state of async operation. Corresponds to <see cref="BeginAuthenticate"/> method. Returns <c>true</c> if the request is authenticated and <see cref="Session"/> is set; otherwise <c>false</c>.
        ///</summary>
        ///<param name="ar">async state of the operation.</param>
        ///<exception cref="ArgumentNullException"><paramref name="ar"/> is null.</exception>
        ///<exception cref="FacebookApiException"></exception>
        public bool EndAuthenticateRequest([NotNull] IAsyncResult ar)
        {
            return TypedAsyncResult<bool>.End(ar, FacebookApi.ExConverter);
        }

        #endregion

        #region Private and protected methods

        long IAuthContext.UserId { get { throw new NotSupportedException(); } }

        /// <summary>
        /// Generates url from the current <see cref="HttpRequest.Url"/> which is useful to pass into <see cref="Authenticate"/> or <see cref="BeginAuthenticate"/> methods as the <c>redirectUri</c> argument.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual string GetCurrentUrl(HttpContext context)
        {
            return context.Request.Url.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped);
        }

        private void SaveSession(HttpContext context)
        {
            ISessionStorage ss = SessionStorage;

            if (ss != null)
                ss.Session = _fbSession;
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

                    _fbSession.Signature = GenerateSignature(_fbSession.ToJsonObject());
                    break;
                case "text/javascript":
                    var obj = JsonObject.CreateFromString(json, CultureInfo.InvariantCulture);
                    if (obj.IsDictionary)
                        FacebookApi.ThrowIfError(obj);

                    throw FacebookApi.UnexpectedResponse(json);
                default:
                    throw FacebookApi.UnexpectedResponse(json);
            }
        }

        #endregion
    }
}
