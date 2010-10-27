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
using System.Web;
using System.Web.SessionState;
using System.Globalization;

namespace Facebook
{
    ///<summary>
    /// Stores facebook <see cref="Facebook.Session"/> information in a cookie compatible with the new Connect JS library.
    ///</summary>
    public class CookieSessionStore : ISessionStorage
    {
        readonly HttpContext _httpContext;
        readonly IAuthContext _authContext;

        ///<summary>
        /// Initializes a new instance of <see cref="CookieSessionStore"/> class with specified <see cref="HttpContext"/> and <see cref="IAuthContext"/> 
        ///</summary>
        ///<param name="httpContext">The http context the response of which is used to store the cookie with session data.</param>
        ///<param name="authContext">The authentication context.</param>
        ///<exception cref="ArgumentNullException">either <paramref name="httpContext"/> or <paramref name="authContext"/> is null.</exception>
        public CookieSessionStore([NotNullAttribute] HttpContext httpContext, [NotNullAttribute] IAuthContext authContext)
        {
            if (httpContext == null)
                throw FacebookApi.Nre("httpContext");
            if (authContext == null)
                throw FacebookApi.Nre("authContext");

            _httpContext = httpContext;
            _authContext = authContext;
        }

        /// <summary>
        /// A key which is used to fetch Session from the storage.
        /// </summary>
        protected string SessionStoreKey;

        ///<summary>
        /// The HttpContext which response is used to store the cookie.
        ///</summary>
        public HttpContext HttpContext { get { return _httpContext; } }

        ///<summary>
        /// The authentication context.
        ///</summary>
        public IAuthContext AuthContext { get { return _authContext; } }

        ///<summary>
        /// A domain to assosiate the cookie with.
        ///</summary>
        public string BaseDomain { get; set; }

        ///<summary>
        /// The name of the cookie to store. Actually the produced name is 'fbs_' + applicationId. This form of cookie is required to be compatible to the connect js api.
        ///</summary>
        public string SessionCookieName
        {
            get { return SessionStoreKey ?? (SessionStoreKey = "fbs_" + AuthContext.AppId); }
        }

        #region ISessionStorage Members

        /// <summary>
        /// Determines if the storage is secure and the library does not need to verify the Session given. Always <c>false</c> for the class.
        /// </summary>
        /// <value>Always <c>false</c>.</value>
        public bool IsSecure { get { return false; } }

        /// <summary>
        /// Fetches from or saves to an instance of facebook <see cref="Facebook.Session"/> using cookie as storage.
        /// </summary>
        /// <exception cref="HttpException">Headers are already written.</exception>
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
    /// Stores facebook <see cref="Facebook.Session"/> information in ASP.NET <see cref="HttpSessionState"/>.
    ///</summary>
    public class AspNetSessionStore : CookieSessionStore, ISessionStorage
    {
        ///<summary>
        /// Initializes a new instance of <see cref="AspNetSessionStore"/> class with specified <see cref="HttpContext"/> and <see cref="IAuthContext"/> 
        ///</summary>
        ///<param name="httpContext">The http context the ASP.NET <see cref="HttpSessionState"/> of which is used to store the session.</param>
        ///<param name="authContext">The authentication context.</param>
        ///<exception cref="ArgumentNullException">either <paramref name="httpContext"/> or <paramref name="authContext"/> is null.</exception>
        public AspNetSessionStore([NotNullAttribute] HttpContext httpContext, [NotNullAttribute] IAuthContext authContext)
            : base(httpContext, authContext)
        {
        }

        /// <summary>
        /// Determines if the storage is secure and the library does not need to verify the Session given. Always <c>true</c> for the class.
        /// </summary>
        /// <value>Always <c>true</c>.</value>
        public new bool IsSecure { get { return true; } }

        /// <summary>
        /// A key which is used to fetch Session from the ASP.NET <see cref="HttpSessionState"/> object.
        /// </summary>
        public new string SessionStoreKey
        {
            get { return SessionCookieName; }
            set { base.SessionStoreKey = value; }
        }

        #region ISessionStorage Members


        /// <summary>
        /// Fetches from or saves to an instance of facebook <see cref="Facebook.Session"/> using the ASP.NET <see cref="HttpSessionState"/> object as storage.
        /// </summary>
        /// <exception cref="HttpException">Headers are already written.</exception>
        public new Session Session
        {
            get
            {
                HttpSessionState httpSession = HttpContext.Session;
                return httpSession == null ? null : httpSession[SessionStoreKey] as Session;
            }
            set
            {
                HttpSessionState httpSession = HttpContext.Session;
                if (httpSession != null)
                    httpSession[SessionStoreKey] = value;
            }
        }

        #endregion
    }
}
