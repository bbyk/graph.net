﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.SessionState;
using System.Collections.Specialized;
using System.Security.Cryptography;

namespace Facebook
{
    public class CanvasUtil
    {
        #region Statics and contants
        static readonly DateTime _unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        static readonly char[] _separator = new char[] { '.' };
        static readonly Comparison<string> _comparison = (s1, s2) => String.CompareOrdinal(s1, s2);
        static readonly Dictionary<string, string> EmptyParams = new Dictionary<string, string>();
        static readonly HashSet<string> _prohibitedKeys = new HashSet<string> { "session", "signed_request", "code" };
        #endregion

        #region Members
        byte[] _appSecretBytes;
        JSONObject _signedRequest;
        Session _fbSession;
        string _sessionStoreKey;
        FacebookAPI _api, _appWideApi;
        readonly IApplicationBindings _bindings;
        #endregion

        public CanvasUtil(IApplicationBindings bindings)
        {
            if (bindings == null)
                throw FacebookAPI.NRE("bindings");
            if (String.IsNullOrEmpty(bindings.AppId))
                throw FacebookAPI.NRE("bindings.AppId");
            if (String.IsNullOrEmpty(bindings.AppSecret))
                throw FacebookAPI.NRE("bindings.AppSecret");
            if (bindings.CanvasPage == null)
                throw FacebookAPI.NRE("bindings.CanvasPage");
            if (bindings.SiteUrl == null)
                throw FacebookAPI.NRE("bindings.SiteUrl");

            _bindings = bindings;
        }

        public string AppId { get { return _bindings.AppId; } }
        public bool HasToken { get { return _fbSession != null; } }
        public bool IsTokenExpired { get { return _fbSession == null ? true : _fbSession.IsExpired; } }

        public FacebookAPI ApiClient
        {
            get { return _api ?? (_api = new FacebookAPI(AccessToken)); }
        }

        public FacebookAPI AppApiClient
        {
            get { return _appWideApi ?? (_appWideApi = new FacebookAPI(AppAccessToken)); }
        }

        public string SessionStoreKey
        {
            get { return _sessionStoreKey ?? (_sessionStoreKey = "fbs_" + AppId); }
        }

        public string GetLoginUrl(Uri currentUrl)
        {
            return GetLoginUrl(currentUrl, EmptyParams);
        }

        public string GetLoginUrl(Uri currentUrl, Dictionary<string, string> @params)
        {
            if (currentUrl == null)
                throw FacebookAPI.NRE("currentUrl");

            string cu = StripAwayProhibitedKeys(currentUrl);

            var p = new Dictionary<string, string> {
                { "api_key", AppId },
                { "cancel_url", cu },
                { "display", "page" },
                { "fbconnect" , "1" },
                { "next" , cu },
                { "return_session", "1" },
                { "session_version", "3" },
                { "v", "1.0" }};

            if (@params != null)
                foreach (var kv in @params)
                    p[kv.Key] = kv.Value;

            return "https://www.facebook.com/login.php" + FacebookAPI.EncodeDictionary(p, true);
        }

        public string GetLogoutUrl(Uri currentUrl)
        {
            return GetLogoutUrl(currentUrl, EmptyParams);
        }

        public string GetLogoutUrl(Uri currentUrl, Dictionary<string, string> @params)
        {
            if (currentUrl == null)
                throw FacebookAPI.NRE("currentUrl");

            string cu = StripAwayProhibitedKeys(currentUrl);

            var p = new Dictionary<string, string> {
                { "next" , cu },
                { "access_token" , AccessToken }};

            foreach (var kv in @params)
                p[kv.Key] = kv.Value;

            return "https://www.facebook.com/login.php" + FacebookAPI.EncodeDictionary(p, true);
        }

        public string ResolveSiteUrl(string relativeUrl)
        {
            if (relativeUrl == null)
                throw FacebookAPI.NRE("relativeUrl");

            return _bindings.SiteUrl.AbsoluteUri + relativeUrl.TrimStart('~', '/');
        }

        public string ResolveCanvasPageUrl(string relativeUrl)
        {
            if (relativeUrl == null)
                throw FacebookAPI.NRE("relativeUrl");

            return _bindings.CanvasPage.AbsoluteUri + relativeUrl.TrimStart('~', '/');
        }

        static string StripAwayProhibitedKeys(Uri currentUrl)
        {
            NameValueCollection nvp = HttpUtility.ParseQueryString(currentUrl.GetComponents(UriComponents.Query, UriFormat.Unescaped));
            var @params = new Dictionary<string, string>(nvp.Count);

            foreach (string name in nvp)
            {
                if (name != null && !_prohibitedKeys.Contains(name))
                    @params[name] = nvp[name];
            }

            return currentUrl.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) + FacebookAPI.EncodeDictionary(@params, true);
        }

        public long UserId
        {
            get { return _fbSession == null ? default(long) : _fbSession.UserId; }
        }

        public bool IsAuthenticated { get { return _fbSession != null; } }

        public bool Authenticate(HttpContext context)
        {
            if (context == null)
                throw FacebookAPI.NRE("context");

            HttpRequest req = context.Request;
            Session session = null;
            // try loading session from signed_request
            var sr = GetSignedRequest(req.QueryString);
            if (sr != null) // sig is good, use the signedRequest
                session = ToFacebookSession(sr);

            // // try to load unsigned session
            string reqSession = req.QueryString["session"];
            if (session == null && !String.IsNullOrEmpty(reqSession))
                session = ValidateSession(JSONObject.CreateFromString(reqSession));

            HttpSessionState httpSession = context.Session;
            if (session == null && httpSession != null)
                session = httpSession[SessionStoreKey] as Session;

            _fbSession = session;

            if (session != null && httpSession != null)
                httpSession[SessionStoreKey] = _fbSession;

            return _fbSession != null;
        }

        /// <exception cref="FacebookAPIException"></exception>
        protected JSONObject GetSignedRequest(NameValueCollection request)
        {
            if (_signedRequest != null)
                return _signedRequest;

            String srStr = request["signed_request"];
            if (String.IsNullOrEmpty(srStr))
                return null;

            return _signedRequest = ParseSignedRequest(srStr);
        }

        /// <exception cref="FacebookAPIException"></exception>
        protected JSONObject ParseSignedRequest(string signedRequest)
        {
            if (String.IsNullOrEmpty(_bindings.AppSecret))
                throw new FacebookAPIException("Config", "AppSecret should be set");

            string[] parts = signedRequest.Split(_separator, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new FacebookAPIException("Canvas", "Incorrect signature format");

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
                throw new FacebookAPIException("Canvas", "Incorrect signature", ex);
            }

            JSONObject data = JSONObject.CreateFromString(payload);
            if (data.IsDictionary && data.Dictionary["algorithm"].String.ToUpperInvariant() != "HMAC-SHA256")
                throw new FacebookAPIException("Canvas", "Unexpected hash algorithm");

            byte[] expectedSignature;
            using (KeyedHashAlgorithm hmac = new HMACSHA256(AppSecretBytes))
                expectedSignature = hmac.ComputeHash(Encoding.ASCII.GetBytes(encodedPayload));

            if (expectedSignature.Length == signature.Length)
            {
                for (int i = 0; i < signature.Length; i++)
                    if (expectedSignature[i] != signature[i]) goto @throw;

                return data;
            }


        @throw: throw new FacebookAPIException("Canvas", "Unexpected signature");
        }

        Session ValidateSession(JSONObject data)
        {
            Session session = null;
            if (data.IsDictionary
                && data.Dictionary.ContainsKey("uid")
                && data.Dictionary.ContainsKey("access_token")
                && data.Dictionary.ContainsKey("sig"))
            {
                string expectedSignature = GenerateSignature(data);
                if (expectedSignature != data.Dictionary["sig"].String)
                    throw new FacebookAPIException("Canvas", "Unexpected signature");

                session = new Session
                {
                    UserId = data.Dictionary["uid"].Integer,
                    OAuthToken = data.Dictionary["access_token"].String,
                    Expires = _unixStart.AddSeconds(data.Dictionary["expires"].Integer),
                };
            }

            return session;
        }

        string GenerateSignature(JSONObject data)
        {
            if (!data.IsDictionary)
                throw new ArgumentException("Should be a dictionary", "data");

            var keys = new List<string>(data.Dictionary.Keys.Where(k => k != "sig"));
            keys.Sort(_comparison);

            var sb = new StringBuilder();
            foreach (string key in keys)
                sb.Append(key).Append('=').Append(data.Dictionary[key]);

            sb.Append(_bindings.AppSecret);

            using (HashAlgorithm md5 = MD5.Create())
                return ByteArrayToHexString(md5.ComputeHash(Encoding.ASCII.GetBytes(sb.ToString())));
        }

        string AccessToken
        {
            get
            {
                if (_fbSession == null)
                    throw new FacebookAPIException("Canvas", "Session is not available");

                if (_fbSession.IsExpired)
                    throw new FacebookAPIException("Canvas", "Token is expired");

                return _fbSession.OAuthToken;
            }
        }

        string AppAccessToken
        {
            get { return AppId + "|" + _bindings.AppSecret; }
        }

        byte[] AppSecretBytes
        {
            get { return _appSecretBytes ?? (_appSecretBytes = Encoding.ASCII.GetBytes(_bindings.AppSecret)); }
        }

        public static void RedirectFromIFrame(HttpContext context, string url)
        {
            if (context == null)
                throw FacebookAPI.NRE("context");
            if (url == null)
                throw FacebookAPI.NRE("url");
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetAllowResponseInBrowserHistory(false);
            context.Response.Write(String.Format(
                    @"<script type=""text/javascript"">if (parent != self) top.location.href = ""{0}""; else self.location.href = ""{0}""</script>",
                    url));
            context.ApplicationInstance.CompleteRequest();
        }

        static Session ToFacebookSession(JSONObject data)
        {
            if (!data.Dictionary.ContainsKey("oauth_token"))
                return null;

            return new Session
            {
                UserId = data.Dictionary["user_id"].Integer,
                OAuthToken = data.Dictionary["oauth_token"].String,
                Expires = _unixStart.AddSeconds(data.Dictionary["expires"].Integer),
            };
        }

        static string ByteArrayToHexString(byte[] bytes)
        {
            var builder = new StringBuilder(3 * bytes.Length);
            foreach (byte t in bytes)
                builder.AppendFormat("{0:x2}", t);
            return builder.ToString().ToLowerInvariant();
        }

        /// <exception cref="FormatException"></exception>
        static byte[] FromBase64String(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            int mod = s.Length % 4;
            s = s.PadRight(s.Length + (mod == 0 ? 0 : 4 - mod), '=');
            return Convert.FromBase64String(s);
        }
    }
}
