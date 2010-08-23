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
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Web.SessionState;

namespace Facebook
{

    enum HttpVerb
    {
        GET,
        POST,
        DELETE
    }

    /// <summary>
    /// Wrapper around the Facebook Graph API. 
    /// </summary>
    public partial class FacebookAPI
    {
        public CultureInfo Culture { get; set; }

        /// <summary>
        /// The access token used to authenticate API calls.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Create a new instance of the API, with public access only.
        /// </summary>
        public FacebookAPI()
        {
        }

        /// <summary>
        /// Create a new instance of the API, using the given token to
        /// authenticate.
        /// </summary>
        /// <param name="token">The access token used for
        /// authentication</param>
        public FacebookAPI(string token)
        {
            if (String.IsNullOrEmpty(token))
                throw new ArgumentNullException("token");

            AccessToken = token;
        }

        /// <summary>
        /// Makes a Facebook Graph API GET request.
        /// </summary>
        /// <param name="relativePath">The path for the call,
        /// e.g. /username</param>
        public JSONObject Get(string relativePath)
        {
            return Call(relativePath, HttpVerb.GET, null);
        }

        /// <summary>
        /// Makes a Facebook Graph API GET request.
        /// </summary>
        /// <param name="relativePath">The path for the call,
        /// e.g. /username</param>
        /// <param name="args">A dictionary of key/value pairs that
        /// will get passed as query arguments.</param>
        public JSONObject Get(string relativePath, 
                              Dictionary<string, string> args)
        {
            return Call(relativePath, HttpVerb.GET, args);
        }

        /// <summary>
        /// Makes a Facebook Graph API DELETE request.
        /// </summary>
        /// <param name="relativePath">The path for the call,
        /// e.g. /username</param>
        public JSONObject Delete(string relativePath)
        {
            return Call(relativePath, HttpVerb.DELETE, null);
        }

        /// <summary>
        /// Makes a Facebook Graph API POST request.
        /// </summary>
        /// <param name="relativePath">The path for the call,
        /// e.g. /username</param>
        /// <param name="args">A dictionary of key/value pairs that
        /// will get passed as query arguments. These determine
        /// what will get set in the graph API.</param>
        public JSONObject Post(string relativePath,
                               Dictionary<string, string> args)
        {
            return Call(relativePath, HttpVerb.POST, args);
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
        private JSONObject Call(string relativePath, 
                                HttpVerb httpVerb,
                                Dictionary<string, string> args)
        {
            Uri baseURL = new Uri("https://graph.facebook.com");
            Uri url = new Uri(baseURL, relativePath);
            if (args == null)
            {
                args = new Dictionary<string, string>();
            }
            if (!string.IsNullOrEmpty(AccessToken))
            {
                args["access_token"] = AccessToken;
            }

            string tmp;
            JSONObject obj = JSONObject.CreateFromString(MakeRequest(url,
                                                                     httpVerb,
                                                                     null,
                                                                     args,
                                                                     out tmp));
            if (obj.IsDictionary && obj.Dictionary.ContainsKey("error"))
            {
                throw ProtocolError(obj);
            }
            return obj;
        }

        /// <summary>
        /// Make an HTTP request, with the given query args
        /// </summary>
        /// <param name="url">The URL of the request</param>
        /// <param name="verb">The HTTP verb to use</param>
        /// <param name="args">Dictionary of key/value pairs that represents
        /// the key/value pairs for the request</param>
        /// <exception cref="FacebookAPIException"></exception>
        /// <exception cref="SecurityException"></exception>
        internal static string MakeRequest(Uri url, HttpVerb httpVerb, CultureInfo ci, Dictionary<string, string> args, out string contentType)
        { 
            if (args != null && args.Keys.Count > 0 && httpVerb == HttpVerb.GET)
            {
                url = new Uri(url.AbsoluteUri + EncodeDictionary(args, true));
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers.Add(HttpRequestHeader.AcceptLanguage, ci == null ? "en" : ci.IetfLanguageTag.ToLowerInvariant());
            request.Method = httpVerb.ToString();

            if (httpVerb == HttpVerb.POST)
            {
                string postData = EncodeDictionary(args, false);

                byte[] postDataBytes = Encoding.ASCII.GetBytes(postData);

                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postDataBytes.Length;

                try
                {
                    using (Stream requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(postDataBytes, 0, postDataBytes.Length);
                    }
                }
                catch (Exception ex)
                {
                    throw NonProtocolError(ex);
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
                    response = response = (HttpWebResponse)ex.Response;
                    contentType = ExtractContentType(response);

                    if (contentType == "text/javascript")
                    {
                        using (response)
                        {
                            return new StreamReader(response.GetResponseStream()).ReadToEnd();
                        }
                    }
                }

                throw NonProtocolError(ex);
            }
            catch (Exception ex)
            {
                throw NonProtocolError(ex);
            }
        }

        /// <summary>
        /// Encode a dictionary of key/value pairs as an HTTP query string.
        /// </summary>
        /// <param name="dict">The dictionary to encode</param>
        /// <param name="questionMark">Whether or not to start it
        /// with a question mark (for GET requests)</param>
        internal static string EncodeDictionary(Dictionary<string, string> dict, bool questionMark)
        {
            StringBuilder sb = new StringBuilder();
            if (questionMark && dict.Count > 0)
            {
                sb.Append('?');
            }
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
            {
                throw MissingContentTypeError();
            }
            return contentType.Split(';')[0];
        }

        internal static Exception ProtocolError(JSONObject obj)
        {
            return new FacebookAPIException(obj.Dictionary["error"]
                                              .Dictionary["type"]
                                              .String,
                                           obj.Dictionary["error"]
                                              .Dictionary["message"]
                                              .String);
        }

        static Exception NonProtocolError(Exception ex)
        {
            return new FacebookAPIException("Server Error", "For more information see the inner exception", ex);
        }

        static Exception MissingContentTypeError()
        {
            return UnexpectedResponseError("Missing Content-Type header");
        }

        internal static Exception UnexpectedResponseError(string response)
        {
            return new FacebookAPIException("Unexpected Response", response);
        }
    }
}
