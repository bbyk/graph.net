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
using System.Text;
using System.Web;
using System.Collections.Specialized;
using System.Security.Cryptography;

namespace Facebook
{
    /// <summary>
    /// Exposes canvas based authentication model. Fits well for IFrame apps.
    /// </summary>
    public class CanvasAuthContext : AuthContextBase, IAuthContext
    {
        #region Statics and contants
        static readonly DateTime s_unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        static readonly char[] s_separator = new[] { '.' };

        ///<summary>
        ///</summary>
        public static readonly Dictionary<string, string> EmptyParams = new Dictionary<string, string>();
        static readonly HashSet<string> s_prohibitedKeys = new HashSet<string> { "session", "signed_request", "code" };
        #endregion

        #region Members
        byte[] _appSecretBytes;
        JsonObject _signedRequest;
        Session _fbSession;
        FacebookApi _api, _appWideApi;
        readonly IApplicationBindings _bindings;

        #endregion

        #region Contructors

        ///<summary>
        /// Initializes a new <see cref="CanvasAuthContext"/> object with specified <paramref name="bindings"/>.
        ///</summary>
        ///<param name="bindings">Application facebook bindings: appid, secret, site url, canvas page.</param>
        ///<exception cref="ArgumentNullException"><paramref name="bindings"/> is null.</exception>
        public CanvasAuthContext([NotNull] IApplicationBindings bindings)
        {
            if (bindings == null)
                throw FacebookApi.Nre("bindings");
            if (String.IsNullOrEmpty(bindings.AppId))
                throw FacebookApi.Nre("bindings.AppId");
            if (String.IsNullOrEmpty(bindings.AppSecret))
                throw FacebookApi.Nre("bindings.AppSecret");
            if (bindings.CanvasPage == null)
                throw FacebookApi.Nre("bindings.CanvasPage");
            if (bindings.SiteUrl == null)
                throw FacebookApi.Nre("bindings.SiteUrl");

            _bindings = bindings;
        }

        #endregion

        #region Public methods and properties

        ///<summary>
        /// See <see cref="IAuthContext.AppId"/>.
        ///</summary>
        public string AppId { get { return _bindings.AppId; } }

        /// <summary>
        /// See <see cref="IAuthContext.AppSecret"/>.
        /// </summary>
        public override string AppSecret { get { return _bindings.AppSecret; } }

        /// <summary>
        /// See <see cref="IAuthContext.Session"/>.
        /// </summary>
        public Session Session { get { return _fbSession; } }

        /// <summary>
        /// See <see cref="IAuthContext.Session"/>.
        /// </summary>
        public string AccessToken
        {
            get
            {
                if (_fbSession == null)
                    throw new FacebookApiException("Canvas", "Session is not available");

                if (_fbSession.IsExpired)
                    throw new FacebookApiException("Canvas", "Token is expired");

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

        /// <summary>
        /// See <see cref="IAuthContext.UserId"/>.
        /// </summary>
        public long UserId
        {
            get { return _fbSession == null ? default(long) : _fbSession.UserId; }
        }

        ///<summary>
        /// See <see cref="IAuthContext.ApiClient"/>.
        ///</summary>
        public FacebookApi ApiClient
        {
            get
            {
                if (_api != null)
                    return _api;

                var api = ApiClientFactory.Create();
                api.AccessToken = AccessToken;

                return (_api = api);
            }
        }

        ///<summary>
        /// See <see cref="IAuthContext.AppApiClient"/>.
        ///</summary>
        public FacebookApi AppApiClient
        {
            get
            {
                if (_appWideApi != null)
                    return _appWideApi;

                var api = ApiClientFactory.Create();
                api.AccessToken = AppAccessToken;

                return (_appWideApi = api);
            }
        }

        ///<summary>
        /// See <see cref="IAuthContext.GetLoginUrl(Uri, Dictionary{String, String})"/>.
        ///</summary>
        ///<param name="nextUrl" />
        ///<returns />
        ///<exception cref="ArgumentNullException"><paramref name="nextUrl"/> is null.</exception>
        public string GetLoginUrl(Uri nextUrl)
        {
            return GetLoginUrl(nextUrl, EmptyParams);
        }

        /// <summary>
        /// See <see cref="IAuthContext.GetLoginUrl(Uri, LoginParams)"/>.
        /// </summary>
        /// <param name="nextUrl" />
        /// <param name="params" />
        /// <returns />
        ///<exception cref="ArgumentNullException"><paramref name="nextUrl"/> is null.</exception>
        public string GetLoginUrl(Uri nextUrl, LoginParams @params)
        {
            if (nextUrl == null)
                throw FacebookApi.Nre("nextUrl");

            var p = new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(@params.CancelUrl))
                p.Add("cancel_url", @params.CancelUrl);
            if (!String.IsNullOrEmpty(@params.ReqPerms))
                p.Add("req_perms", @params.ReqPerms);
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
        public string GetLoginUrl(Uri nextUrl, Dictionary<string, string> @params)
        {
            if (nextUrl == null)
                throw FacebookApi.Nre("nextUrl");

            string next = StripAwayProhibitedKeys(nextUrl);

            var p = new Dictionary<string, string>
            {
                {"api_key", AppId},
                {"cancel_url", next},
                {"display", "page"},
                {"fbconnect", "1"},
                {"next", next},
                {"return_session", "1"},
                {"session_version", "3"},
                {"v", "1.0"}
            };

            if (@params != null)
                foreach (var kv in @params)
                    p[kv.Key] = kv.Value;

            return "https://www.facebook.com/login.php?" + FacebookApi.EncodeDictionary(p);
        }

        /// <summary>
        /// See <see cref="IAuthContext.GetLogoutUrl"/>
        /// </summary>
        /// <param name="nextUrl" />
        /// <returns />
        ///<exception cref="ArgumentNullException"><paramref name="nextUrl"/> is null.</exception>
        public string GetLogoutUrl(Uri nextUrl)
        {
            return GetLogoutUrl(nextUrl, EmptyParams);
        }

        /// <summary>
        /// See <see cref="IAuthContext.GetLogoutUrl"/>
        /// </summary>
        /// <param name="nextUrl" />
        /// <param name="params" />
        /// <returns />
        ///<exception cref="ArgumentNullException"><paramref name="nextUrl"/> is null.</exception>
        public string GetLogoutUrl(Uri nextUrl, Dictionary<string, string> @params)
        {
            if (nextUrl == null)
                throw FacebookApi.Nre("nextUrl");

            string next = StripAwayProhibitedKeys(nextUrl);

            var p = new Dictionary<string, string> {
                                                       { "next" , next },
                                                       { "access_token" , AccessToken }};

            foreach (var kv in @params ?? EmptyParams)
                p[kv.Key] = kv.Value;

            return "https://www.facebook.com/logout.php?" + FacebookApi.EncodeDictionary(p);
        }

        /// <summary>
        /// Generates absolute url for a relative url on the site (e.g. http://localhost/graph.net/default.aspx).
        /// </summary>
        /// <param name="relativeUrl">a relative url to convert to absolute (e.g. ~/default.aspx).</param>
        /// <returns>An absolute url for the relative url on the site.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="relativeUrl"/> is null.</exception>
        public string ResolveSiteUrl([NotNull] string relativeUrl)
        {
            if (relativeUrl == null)
                throw FacebookApi.Nre("relativeUrl");

            return _bindings.SiteUrl.TrimEnd('/') + '/' + relativeUrl.TrimStart('~', '/');
        }

        /// <summary>
        /// Generates absolute url for a relative url on facebook application canvas (e.g. http://apps.facebook.com/graphdotnet/default.aspx).
        /// </summary>
        /// <param name="relativeUrl">a relative url to convert to absolute (e.g. ~/default.aspx).</param>
        /// <returns>An absolute url for the relative url on facebook application canvas.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="relativeUrl"/> is null.</exception>
        public string ResolveCanvasPageUrl([NotNull] string relativeUrl)
        {
            if (relativeUrl == null)
                throw FacebookApi.Nre("relativeUrl");

            return _bindings.CanvasPage.TrimEnd('/') + '/' + relativeUrl.TrimStart('~', '/');
        }

        internal static string StripAwayProhibitedKeys(Uri currentUrl)
        {
            NameValueCollection nvp = HttpUtility.ParseQueryString(currentUrl.GetComponents(UriComponents.Query, UriFormat.Unescaped));
            var @params = new Dictionary<string, string>(nvp.Count);

            foreach (string name in nvp)
            {
                if (name != null && !s_prohibitedKeys.Contains(name))
                    @params[name] = nvp[name];
            }

            return currentUrl.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) + "?" + FacebookApi.EncodeDictionary(@params);
        }

        /// <summary>
        /// See <see cref="IAuthContext.IsAuthenticated"/>.
        /// </summary>
        public bool IsAuthenticated { get { return _fbSession != null && !_fbSession.IsExpired; } }

        /// <summary>
        /// Authenticates current request. Returns <c>true</c> if the request is authenticated and an instance of <see cref="Session"/> is set; otherwise <c>false</c>.
        /// </summary>
        /// <param name="context">the current request context information.</param>
        /// <returns><c>true</c> if the request is authenticated and an instance of <see cref="Session"/> is set; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
        public bool Authenticate([NotNull] HttpContext context)
        {
            if (context == null)
                throw FacebookApi.Nre("context");

            bool saveSession = true;
            HttpRequest req = context.Request;
            Session session = null;
            // try loading session from signed_request
            var sr = GetSignedRequest(req.QueryString);
            if (sr != null) // sig is good, use the signedRequest
                session = ToFacebookSession(sr);

            // // try to load unsigned session
            string reqSession = req.QueryString["session"];
            if (session == null && !String.IsNullOrEmpty(reqSession))
                session = ValidateSession(JsonObject.CreateFromString(reqSession, Culture));

            ISessionStorage ss = SessionStorage;
            if (session == null && ss != null)
            {
                session = ss.Session;
                if (session != null
                    && !ss.IsSecure
                    && session.Signature != GenerateSignature(session.ToJsonObject()))
                {
                    session = null;
                }

                saveSession = session == null;
            }

            _fbSession = session;

            if (ss != null && saveSession)
                ss.Session = _fbSession;

            return _fbSession != null;
        }

        #endregion

        #region Private methods

        /// <exception cref="FacebookApiException"></exception>
        JsonObject GetSignedRequest(NameValueCollection request)
        {
            if (_signedRequest != null)
                return _signedRequest;

            String srStr = request["signed_request"];
            if (String.IsNullOrEmpty(srStr))
                return null;

            return _signedRequest = ParseSignedRequest(srStr);
        }

        /// <exception cref="FacebookApiException"></exception>
        JsonObject ParseSignedRequest(string signedRequest)
        {
            if (String.IsNullOrEmpty(_bindings.AppSecret))
                throw new FacebookApiException("Config", "AppSecret should be set");

            string[] parts = signedRequest.Split(s_separator, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new FacebookApiException("Canvas", "Incorrect signature format");

            string encodedSignature = parts[0];
            string encodedPayload = parts[1];

            string payload;
            byte[] signature;
            try
            {
                signature = FromBase64String(encodedSignature);
                payload = Encoding.ASCII.GetString(FromBase64String(encodedPayload));
            }
            catch (FormatException ex)
            {
                throw new FacebookApiException("Canvas", "Incorrect signature", ex);
            }

            var data = JsonObject.CreateFromString(payload, Culture);
            if (data.IsDictionary && data.Dictionary["algorithm"].String.ToUpperInvariant() != "HMAC-SHA256")
                throw new FacebookApiException("Canvas", "Unexpected hash algorithm");

            byte[] expectedSignature;
            using (KeyedHashAlgorithm hmac = new HMACSHA256(AppSecretBytes))
                expectedSignature = hmac.ComputeHash(Encoding.ASCII.GetBytes(encodedPayload));

            if (expectedSignature.Length == signature.Length)
            {
                for (int i = 0; i < signature.Length; i++)
                    if (expectedSignature[i] != signature[i]) goto @throw;

                return data;
            }


        @throw: throw new FacebookApiException("Canvas", "Unexpected signature");
        }

        Session ValidateSession(JsonObject data)
        {
            if (!data.IsDictionary)
                throw new ArgumentException("Should be a dictionary", "data");

            Session session = null;
            if (data.IsDictionary
                && data.Dictionary.ContainsKey("uid")
                && data.Dictionary.ContainsKey("access_token")
                && data.Dictionary.ContainsKey("sig"))
            {
                string expectedSignature = GenerateSignature(data);
                if (expectedSignature != data.Dictionary["sig"].String)
                    throw new FacebookApiException("Canvas", "Unexpected signature");

                return Session.FromJsonObject(data);
            }

            return session;
        }

        /// <summary>
        /// See <see cref="IAuthContext.AppAccessToken"/>.
        /// </summary>
        public string AppAccessToken
        {
            get { return AppId + "|" + _bindings.AppSecret; }
        }

        byte[] AppSecretBytes
        {
            get { return _appSecretBytes ?? (_appSecretBytes = Encoding.ASCII.GetBytes(_bindings.AppSecret)); }
        }

        #endregion

        #region Static methods
        /// <summary>
        /// Generates and writes to response a javascript code to redirect to a url from iframe so that the url will be in browser's address bar. Calling the method completes current request.
        /// </summary>
        /// <param name="context">Http context the <see cref="HttpResponse"/> of which is used to issue the js.</param>
        /// <param name="url">an url to redirect to.</param>
        /// <exception cref="ArgumentNullException">either <paramref name="context"/> or <paramref name="url"/> is null.</exception>
        public static void RedirectFromIFrame([NotNull] HttpContext context, [NotNull] string url)
        {
            if (context == null)
                throw FacebookApi.Nre("context");
            if (url == null)
                throw FacebookApi.Nre("url");
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetAllowResponseInBrowserHistory(false);
            context.Response.Write(String.Format(
                @"<script type=""text/javascript"">if (parent != self) top.location.href = ""{0}""; else self.location.href = ""{0}""</script>",
                url));
            context.ApplicationInstance.CompleteRequest();
        }

        Session ToFacebookSession(JsonObject data)
        {
            if (!data.Dictionary.ContainsKey("oauth_token"))
                return null;

            var expires = data.Dictionary["expires"].Integer;

            var sess = new Session
            {
                UserId = data.Dictionary["user_id"].Integer,
                OAuthToken = data.Dictionary["oauth_token"].String,
                // if user granted 'offline_access' permission, the 'expires' value is 0.
                Expires = expires == 0 ? DateTime.MaxValue : s_unixStart.AddSeconds(expires),
            };

            sess.Signature = GenerateSignature(sess.ToJsonObject());
            return sess;
        }

        #endregion
    }
}
