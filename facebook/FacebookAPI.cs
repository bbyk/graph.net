#region Facebook's boilerplate notice
/*
 * Copyright 2010 Facebook, Inc.
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

// THIS FILE IS MODIFIED SINCE FORKING FROM http://github.com/facebook/csharp-sdk/commit/52cf2493349494b783e321e0ea22335481b1c058 //

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
using System.Net;
using System.IO;
using System.Web;
using System.Globalization;
using System.Security;

namespace Facebook
{
    enum HttpVerb
    {
        Get,
        Post,
        Delete
    }

    /// <summary>
    /// Wrapper around the Facebook Graph API. 
    /// </summary>
    public partial class FacebookApi
    {
        CultureInfo _ci;
        TimeSpan? _timeout;
        static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(100);

        ///<summary>
        /// Gets or sets the proxy information for api requests.
        ///</summary>
        public IWebProxy Proxy { get; set; }

        ///<summary>
        /// Gets or sets timeout for api requests.
        ///</summary>
        ///<exception cref="ArgumentException">cannot be less than zero.</exception>
        public TimeSpan Timeout
        {
            get { return _timeout.HasValue ? _timeout.Value : s_defaultTimeout; }
            set { if (value <= TimeSpan.Zero) throw new ArgumentException("Timeout should be greater than zero.", "value"); _timeout = value; }
        }

        ///<summary>
        /// Current locale for graph calls. Facebook graph is locale sensitive. If not provided, <see cref="CultureInfo.CurrentCulture"/> is used.
        ///</summary>
        public CultureInfo Culture
        {
            [NotNull]
            get { return _ci ?? CultureInfo.CurrentCulture; }
            set { _ci = value; }
        }

        /// <summary>
        /// The access token used to authenticate API calls.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Create a new instance of the API, with public access only.
        /// </summary>
        public FacebookApi()
        {
        }

        ///<summary>
        /// Create a new instance of the API, with specified access token.
        ///</summary>
        ///<param name="token">An access token for api requests. If <c>null</c>, only public information is available (e.g. http://graph.facebook.com/710740487)</param>
        public FacebookApi([CanBeNull] string token)
            : this(token, CultureInfo.CurrentCulture)
        {
        }

        /// <summary>
        /// Create a new instance of the API, using the given token and culture (locale).
        /// </summary>
        /// <param name="token">An access token for api requests. If <c>null</c>, only public information is available (e.g. http://graph.facebook.com/710740487).</param>
        /// <param name="culture"><see cref="Culture"/> for more information.</param>
        public FacebookApi([CanBeNull] string token, [CanBeNull] CultureInfo culture)
        {
            AccessToken = token;
            Culture = culture ?? CultureInfo.CurrentCulture;
        }

        /// <summary>
        /// Makes a Facebook API GET request.
        /// </summary>
        /// <param name="relativePath">The path for the call, e.g. /username</param>
        /// <exception cref="FacebookApiException"></exception>
        /// <exception cref="ArgumentNullException"><paramref name="relativePath"/> is null.</exception>
        public JsonObject Get([NotNull] string relativePath)
        {
            if (relativePath == null)
                throw Nre("relativePath");
            return Call(relativePath, HttpVerb.Get, null);
        }

        /// <summary>
        /// Makes a Facebook API GET request. If <paramref name="relativePath"/> is <c>null</c>, <paramref name="args"/> should contain <c>ids</c> or another facebook blessed approach to mean it is a batch call.
        /// </summary>
        /// <param name="relativePath">The path for the call, e.g. /username</param>
        /// <param name="args">A dictionary of key/value pairs that will get passed as query arguments.</param>
        /// <exception cref="FacebookApiException">an exception occurred during the call.</exception>
        public JsonObject Get([CanBeNull] string relativePath, [CanBeNull] Dictionary<string, string> args)
        {
            return Call(relativePath, HttpVerb.Get, args);
        }

        /// <summary>
        /// Makes a Facebook API DELETE request.
        /// </summary>
        /// <param name="relativePath">The path for the call, e.g. /username.</param>
        /// <exception cref="FacebookApiException">an exception occurred during the call.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="relativePath"/> is null.</exception>
        public JsonObject Delete([CanBeNull] string relativePath)
        {
            if (relativePath == null)
                throw Nre("relativePath");
            return Call(relativePath, HttpVerb.Delete, null);
        }

        /// <summary>
        /// Makes a Facebook API DELETE request. If <paramref name="relativePath"/> is <c>null</c>, <paramref name="args"/> should contain <c>ids</c> or another facebook blessed approach to mean it is a batch call.
        /// </summary>
        /// <param name="relativePath">The path for the call, e.g. /username.</param>
        /// <param name="args">A dictionary of key/value pairs that will get passed as query arguments. These determine what will get set in the graph API.</param>
        /// <exception cref="FacebookApiException">an exception occurred during the call.</exception>
        public JsonObject Delete([CanBeNull] string relativePath, [CanBeNull] Dictionary<string, string> args)
        {
            return Call(relativePath, HttpVerb.Delete, args);
        }

        /// <summary>
        /// Makes a Facebook API POST request. If <paramref name="relativePath"/> is <c>null</c>, <paramref name="args"/> should contain <c>ids</c> or another facebook blessed approach to mean it is a batch call.
        /// </summary>
        /// <param name="relativePath">The path for the call, e.g. /username</param>
        /// <param name="args">A dictionary of key/value pairs that will get passed as query arguments. These determine what will get set in the graph API.</param>
        /// <exception cref="FacebookApiException">an exception occurred during the call.</exception>
        public JsonObject Post([CanBeNull] string relativePath, [CanBeNull] Dictionary<string, string> args)
        {
            return Call(relativePath, HttpVerb.Post, args);
        }

        /// <summary>
        /// Makes a Facebook Graph API Call.
        /// </summary>
        /// <param name="relativePath">The path for the call, 
        /// e.g. /username</param>
        /// <param name="httpVerb">The HTTP verb to use, e.g.
        /// GET, POST, DELETE</param>
        /// <param name="args">A dictionary of key/value pairs that
        /// will get passed as query arguments.</param>
        private JsonObject Call(string relativePath,
                                HttpVerb httpVerb,
                                Dictionary<string, string> args)
        {
            relativePath = (relativePath ?? String.Empty).TrimStart('/');
            var url = GetApiBaseUrl(relativePath) + relativePath;

            if (args == null)
                args = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(AccessToken))
                args["access_token"] = AccessToken;
            if (url.StartsWith("https://api"))
                args["format"] = "json";

            string tmp;
            var obj = JsonObject.CreateFromString(Request(url,
                                                              httpVerb,
                                                              args,
                                                              out tmp), Culture);
            if (obj.IsDictionary)
                ThrowIfError(obj);
            return obj;
        }

        /// <summary>
        /// Make an HTTP request, with the given query args
        /// </summary>
        /// <param name="url">The URL of the request</param>
        /// <param name="httpVerb">The HTTP verb to use</param>
        /// <param name="args">Dictionary of key/value pairs that represents
        /// the key/value pairs for the request</param>
        /// <param name="contentType"></param>
        /// <exception cref="FacebookApiException"></exception>
        /// <exception cref="SecurityException"></exception>
        internal string Request(string url, HttpVerb httpVerb, Dictionary<string, string> args, out string contentType)
        { 
            if (args != null && args.Keys.Count > 0 && httpVerb == HttpVerb.Get)
            {
                url = url+ (url.Contains("?") ? "&" : "?") + EncodeDictionary(args);
            }

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Proxy = Proxy;
            request.Timeout = (int)Timeout.TotalMilliseconds;
            request.Headers.Add(HttpRequestHeader.AcceptLanguage, Culture.IetfLanguageTag.ToLowerInvariant());
            request.Accept = "text/javascript";
            request.Method = httpVerb.ToString();

            if (httpVerb == HttpVerb.Post)
            {
                string postData = EncodeDictionary(args);
                byte[] postDataBytes = Encoding.UTF8.GetBytes(postData);

                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postDataBytes.Length;

                try
                {
                    using (Stream requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(postDataBytes, 0, postDataBytes.Length);
                    }
                }
                catch(WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.Timeout)
                        throw OperationTimeout(ex);

                    throw;
                }
                catch (Exception ex)
                {
                    throw TransportError(ex);
                }
            }

            HttpWebResponse response;
            try
            {
                using (response = (HttpWebResponse)request.GetResponse())
                {
                    contentType = ExtractContentType(response);
                    return new StreamReader(response.GetResponseStream()).ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    response = (HttpWebResponse)ex.Response;
                    contentType = ExtractContentType(response);

                    if (contentType == "text/javascript")
                    {
                        using (response)
                        {
                            return new StreamReader(response.GetResponseStream()).ReadToEnd();
                        }
                    }
                }

                throw TransportError(ex);
            }
            catch (Exception ex)
            {
                throw TransportError(ex);
            }
        }

        /// <summary>
        /// Encode a dictionary of key/value pairs as an HTTP query string.
        /// </summary>
        /// <param name="dict">The dictionary to encode</param>
        public static string EncodeDictionary(Dictionary<string, string> dict)
        {
            var sb = new StringBuilder();

            bool first = true;
            foreach (KeyValuePair<string, string> kvp in dict)
            {
                if (first) first = false;
                else sb.Append('&');
                sb.Append(HttpUtility.UrlEncode(kvp.Key));
                sb.Append('=');
                sb.Append(HttpUtility.UrlEncode(kvp.Value));
            }
            return sb.ToString();
        }

        static string ExtractContentType(HttpWebResponse response)
        {
            string contentType = response.ContentType;
            if (String.IsNullOrEmpty(contentType))
                throw MissingContentType();
            return contentType.Split(';')[0];
        }

        internal static void ThrowIfError(JsonObject obj)
        {
            if (obj.Dictionary.ContainsKey("error"))
                throw new FacebookApiException( obj.Dictionary["error"].Dictionary["type"].String,
                                                obj.Dictionary["error"].Dictionary["message"].String);
            if (obj.Dictionary.ContainsKey("error_code"))
                throw new FacebookApiException( obj.Dictionary["error_code"].String,
                                                obj.Dictionary["error_msg"].String);
        }

        static Exception TransportError(Exception ex)
        {
            return new FacebookApiException("Server Error", "For more information see the inner exception", ex);
        }

        static Exception MissingContentType()
        {
            return UnexpectedResponse("Missing Content-Type header");
        }

        internal static ArgumentNullException Nre(string paramName)
        {
            return new ArgumentNullException(paramName);
        }

        private static TimeoutException OperationTimeout(Exception ex)
        {
            return new TimeoutException("Operation timed out.", ex);
        }

        internal static Exception UnexpectedResponse(string response)
        {
            return new FacebookApiException("Unexpected Response", response);
        }

        static string GetApiBaseUrl(string relativeUrl)
        {
            return (relativeUrl ?? String.Empty).StartsWith("method/") ? "https://api.facebook.com/" : "https://graph.facebook.com/";
        }
    }
}
