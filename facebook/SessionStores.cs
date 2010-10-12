using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.SessionState;
using System.Globalization;

namespace Facebook
{
    ///<summary>
    /// Stores facebook session information in cookie compatible with new Connect JS library.
    ///</summary>
    public class CookieSessionStore : ISessionStorage
    {
        static readonly DateTime s_unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// </summary>
        protected string SessionStoreKey;

        ///<summary>
        ///</summary>
        public HttpContext HttpContext { get; set; }

        ///<summary>
        ///</summary>
        public IAuthContext AuthContext { get; set; }

        ///<summary>
        ///</summary>
        public string BaseDomain { get; set; }

        ///<summary>
        ///</summary>
        public string SessionCookieName
        {
            get
            {
                EnsureInitialized();
                return SessionStoreKey ?? (SessionStoreKey = "fbs_" + AuthContext.AppId);
            }
        }

        /// <summary>
        /// </summary>
        protected void EnsureInitialized()
        {
            if (HttpContext == null)
                throw FacebookApi.Nre("HttpContext");

            if (AuthContext == null)
                throw FacebookApi.Nre("AuthContext");
        }

        #region ISessionStorage Members

        /// <summary>
        /// </summary>
        public bool IsSecure { get { return false; } }

        /// <summary>
        /// </summary>
        /// <exception cref="HttpException">Headers are already written.</exception>
        /// <exception cref="ArgumentNullException">The object was not properly initialized. HttpContext and AuthContext properties should be set.</exception>
        public Session Session
        {
            get
            {
                string cookieName = SessionCookieName;
                HttpCookie cookie = HttpContext.Request.Cookies[cookieName];
                Session session = null;
                
                if (cookie != null
                    && cookie.Value != null
                    && cookie.Value.Length > 2
                    && cookie.Value.StartsWith("\"")
                    && cookie.Value.EndsWith("\""))
                {
                    try
                    {
                        session = Session.FromJsonObject(JsonObject.Create(
                                HttpUtility.ParseQueryString(cookie.Value.Substring(1, cookie.Value.Length - 2)), CultureInfo.InvariantCulture));
                    }
                    catch (Exception ex)
                    {
                        var exProcessor = AuthContext.ExProcessor;
                        if (exProcessor != null)
                            exProcessor(ex);
                    }
                }

                return session;
            }
            set
            {
                string cookieName = SessionCookieName;
                HttpCookie cookie = HttpContext.Request.Cookies[cookieName];

                // nothing is changed, return
                if (value == null && cookie == null)
                    return;

                if (value == null)
                {
                    cookie = new HttpCookie(cookieName)
                    {
                        Value = "deleted",
                        Expires = DateTime.UtcNow.AddHours(-1),
                        Path = "/",
                        Domain = BaseDomain,
                    };
                }
                else
                {
                    cookie = new HttpCookie(cookieName)
                    {
                        Value = "\"" + FacebookApi.EncodeDictionary(value.ToDictionary()) + "\"",
                        Expires = value.Expires,
                        Path = "/",
                        Domain = BaseDomain,
                    };
                }

                HttpContext.Response.SetCookie(cookie);
            }
        }

        #endregion
    }

    ///<summary>
    ///</summary>
    public class AspNetSessionStore : CookieSessionStore, ISessionStorage
    {
        /// <summary>
        /// </summary>
        public new bool IsSecure { get { return true; } }

        ///<summary>
        ///</summary>
        public new string SessionStoreKey
        {
            get { return SessionCookieName; }
            set { base.SessionStoreKey = value; }
        }

        #region ISessionStorage Members

        ///<summary>
        ///</summary>
        public new Session Session
        {
            get
            {
                EnsureInitialized();
                HttpSessionState httpSession = HttpContext.Session;
                return httpSession == null ? null : httpSession[SessionStoreKey] as Session;
            }
            set
            {
                EnsureInitialized();
                HttpSessionState httpSession = HttpContext.Session;
                if (httpSession != null)
                    httpSession[SessionStoreKey] = value;
            }
        }

        #endregion
    }
}
