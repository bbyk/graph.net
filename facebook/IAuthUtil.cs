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
using System.Globalization;
using System.Net;

namespace Facebook
{
    ///<summary>
    /// Represents generic properties and methods of the authentication contexts used by Facebook: <see cref="CanvasAuthContext"/> and <see cref="OAuthContext"/>.
    ///</summary>
    public interface IAuthContext
    {
        ///<summary>
        /// Indicates authentication status.
        ///</summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Facebook id of a user on behalf of which the authentication is carried out.
        /// </summary>
        /// <exception cref="NotSupportedException">In case of OAuth and <see cref="OAuthContext"/> facebook id is not passed in context of authentication. Client code should use <see cref="FacebookApi.Get(String)"/> method to fetch user's profile out fo graph using <c>me</c> instead of facebook id.</exception>
        long UserId { get; }

        ///<summary>
        /// Current application id.
        ///</summary>
        [NotNull]
        string AppId { get; }

        /// <summary>
        /// Gets an instance of facebook <see cref="Session"/> object. Can be null, provided either authentication is not carried out or failed.
        /// </summary>
        [CanBeNull]
        Session Session { get; }

        ///<summary>
        /// Application secret key.
        ///</summary>
        [NotNull]
        string AppSecret { get; }

        ///<summary>
        /// Returns the access token associated with the current authentication context.
        ///</summary>
        /// <exception cref="FacebookApiException">Session is either null or expired.</exception>
        [NotNull]
        string AccessToken { get; }

        ///<summary>
        /// Returns the access token associated with the current application.
        ///</summary>
        [NotNull]
        string AppAccessToken { get; }

        ///<summary>
        /// UTC datatime when the current <see cref="AccessToken"/> is expired.
        ///</summary>
        DateTime Expires { get; }

        ///<summary>
        /// Gets an instance of <see cref="FacebookApi"/> class initialized with the current authentication context data.
        ///</summary>
        /// <exception cref="FacebookApiException">Session is either null or expired.</exception>
        [NotNull]
        FacebookApi ApiClient { get; }

        ///<summary>
        /// Gets an instance of <see cref="FacebookApi"/> class initialized with the application credentials.
        ///</summary>
        [NotNull]
        FacebookApi AppApiClient { get; }

        ///<summary>
        /// Gets or sets an api client factory. The property always returns a not-null value. If it is set to null via its <c>set</c> accessor, its <c>get</c> accessor will return a default factory.
        ///</summary>
        IFacebookApiFactory ApiClientFactory { [NotNull] get; [CanBeNull] set; }

        ///<summary>
        /// A callback to process unexpected exceptions when authenticating on behalf of the client code (e.g. logging).
        ///</summary>
        [CanBeNull]
        Action<Exception> ExProcessor { get; set; }

        ///<summary>
        /// Produces an url to redirect to in order to authenticate user using <see cref="Dictionary{String, String}"/>
        ///</summary>
        ///<param name="nextUrl">Upon complete, the url to redirect to.</param>
        ///<param name="params">Various params such as: <c>req_perms</c>, <c>display</c>, <c>scope</c> etc.</param>
        ///<returns>A url on Facebook to redirect to.</returns>
        ///<exception cref="ArgumentNullException"><paramref name="nextUrl"/> is null.</exception>
        string GetLoginUrl([NotNull] Uri nextUrl, [CanBeNull] Dictionary<string, string> @params);

        ///<summary>
        /// Produces an url to redirect to in order to authenticate user using strong-typed <see cref="LoginParams" /> object.
        ///</summary>
        ///<param name="nextUrl">Upon complete, the url to redirect to.</param>
        ///<param name="params">Strong-typed parameters representation.</param>
        ///<returns>A url on Facebook to redirect to.</returns>
        ///<exception cref="ArgumentNullException"><paramref name="nextUrl"/> is null.</exception>
        string GetLoginUrl([NotNull] Uri nextUrl, [NotNull] LoginParams @params);

        /// <summary>
        /// Produces an url to log users out from Facebook.
        /// </summary>
        /// <returns>A url on Facebook to redirect to.</returns>
        ///<exception cref="ArgumentNullException"><paramref name="nextUrl"/> is null.</exception>
        string GetLogoutUrl([NotNull] Uri nextUrl, [CanBeNull] Dictionary<string, string> @params);
    }

    ///<summary>
    /// Creates and pre-initializes instances of <see cref="FacebookApi"/> class.
    ///</summary>
    public interface IFacebookApiFactory
    {
        ///<summary>
        /// Creates a new pre-initialized instance of <see cref="FacebookApi"/> class.
        ///</summary>
        ///<returns></returns>
        [NotNull]
        FacebookApi Create();

        ///<summary>
        /// Gets or sets the proxy information for api requests.
        ///</summary>
        [CanBeNull]
        IWebProxy Proxy { get; set; }

        ///<summary>
        /// Gets or sets timeout for api requests. Default timeout is 100 sec.
        ///</summary>
        ///<exception cref="ArgumentException">cannot be less than zero.</exception>
        TimeSpan Timeout { get; set; }

        ///<summary>
        /// Current locale for graph calls. Facebook graph is locale sensitive. If not provided or set to null, <see cref="CultureInfo.CurrentCulture"/> is used.
        ///</summary>
        CultureInfo Culture { [NotNull] get; [CanBeNull] set; }
    }
}
