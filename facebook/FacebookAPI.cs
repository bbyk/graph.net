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
using System.Linq;
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

        ///<summary>
        ///</summary>
        public CultureInfo Culture
        {
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
        ///</summary>
        ///<param name="token"></param>
        public FacebookApi(string token)
            : this(token, CultureInfo.CurrentCulture)
        {
        }

        /// <summary>
        /// Create a new instance of the API, using the given token to
        /// authenticate.
        /// </summary>
        /// <param name="token">The access token used for
        /// authentication</param>
        /// <param name="culture"></param>
        public FacebookApi(string token, CultureInfo culture)
        {
            AccessToken = token;
            Culture = culture ?? CultureInfo.CurrentCulture;
        }

        /// <summary>
        /// Makes a Facebook Graph API GET request.
        /// </summary>
        /// <param name="relativePath">The path for the call,
        /// e.g. /username</param>
        public JsonObject Get(string relativePath)
        {
            return Call(relativePath, HttpVerb.Get, null);
        }

        /// <summary>
        /// Makes a Facebook Graph API GET request.
        /// </summary>
        /// <param name="relativePath">The path for the call,
        /// e.g. /username</param>
        /// <param name="args">A dictionary of key/value pairs that
        /// will get passed as query arguments.</param>
        public JsonObject Get(string relativePath, 
                              Dictionary<string, string> args)
        {
            return Call(relativePath, HttpVerb.Get, args);
        }

        /// <summary>
        /// Makes a Facebook Graph API DELETE request.
        /// </summary>
        /// <param name="relativePath">The path for the call,
        /// e.g. /username</param>
        public JsonObject Delete(string relativePath)
        {
            return Call(relativePath, HttpVerb.Delete, null);
        }

        /// <summary>
        /// Makes a Facebook Graph API POST request.
        /// </summary>
        /// <param name="relativePath">The path for the call,
        /// e.g. /username</param>
        /// <param name="args">A dictionary of key/value pairs that
        /// will get passed as query arguments. These determine
        /// what will get set in the graph API.</param>
        public JsonObject Post(string relativePath,
                               Dictionary<string, string> args)
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
            var baseUrl = new Uri("https://graph.facebook.com");
            var url = new Uri(baseUrl, relativePath);
            if (args == null)
            {
                args = new Dictionary<string, string>();
            }
            if (!string.IsNullOrEmpty(AccessToken))
            {
                args["access_token"] = AccessToken;
            }

            string tmp;
            var obj = JsonObject.CreateFromString(MakeRequest(url,
                                                              httpVerb,
                                                              Culture,
                                                              args,
                                                              out tmp), Culture);
            if (obj.IsDictionary && obj.Dictionary.ContainsKey("error"))
            {
                throw GraphError(obj);
            }
            return obj;
        }

        /// <summary>
        /// Make an HTTP request, with the given query args
        /// </summary>
        /// <param name="url">The URL of the request</param>
        /// <param name="httpVerb">The HTTP verb to use</param>
        /// <param name="culture"></param>
        /// <param name="args">Dictionary of key/value pairs that represents
        /// the key/value pairs for the request</param>
        /// <param name="contentType"></param>
        /// <exception cref="FacebookApiException"></exception>
        /// <exception cref="SecurityException"></exception>
        internal static string MakeRequest(Uri url, HttpVerb httpVerb, CultureInfo culture, Dictionary<string, string> args, out string contentType)
        { 
            if (args != null && args.Keys.Count > 0 && httpVerb == HttpVerb.Get)
            {
                url = new Uri(url.AbsoluteUri + "?" + EncodeDictionary(args));
            }

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers.Add(HttpRequestHeader.AcceptLanguage, culture.IetfLanguageTag.ToLowerInvariant());
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
        internal static string EncodeDictionary(Dictionary<string, string> dict)
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

        internal static Exception GraphError(JsonObject obj)
        {
            return new FacebookApiException(obj.Dictionary["error"]
                                              .Dictionary["type"]
                                              .String,
                                           obj.Dictionary["error"]
                                              .Dictionary["message"]
                                              .String);
        }

        static Exception TransportError(Exception ex)
        {
            return new FacebookApiException("Server Error", "For more information see the inner exception", ex);
        }

        static Exception MissingContentType()
        {
            return UnexpectedResponse("Missing Content-Type header");
        }

        internal static Exception Nre(string paramName)
        {
            return new ArgumentNullException(paramName);
        }

        private static Exception OperationTimeout(Exception ex)
        {
            return new TimeoutException("Operation timed out.", ex);
        }

        internal static Exception UnexpectedResponse(string response)
        {
            return new FacebookApiException("Unexpected Response", response);
        }
    }
}
