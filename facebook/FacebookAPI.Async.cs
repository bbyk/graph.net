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
using System.Net;
using System.Text;
using System.IO;

namespace Facebook
{
    public partial class FacebookApi
    {
        internal readonly static Func<Exception, Exception> ExConverter = TransportError;
        internal class ResponseData
        {
            internal string Json;
            internal String ContentType;
        }

        internal IAsyncResult BeginRequest(
            string url,
            HttpVerb httpVerb,
            Dictionary<string, string> args,
            AsyncCallback cb, object state)
        {
            if (args != null && args.Keys.Count > 0 && httpVerb == HttpVerb.Get)
                url = url+ (url.Contains("?") ? "&" : "?") + EncodeDictionary(args);

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Proxy = Proxy;
            request.Headers.Add(HttpRequestHeader.AcceptLanguage, Culture.IetfLanguageTag.ToLowerInvariant());
            request.Method = httpVerb.ToString();
            var tar = new TypedAsyncResult<ResponseData>(cb, state);

            Action beginGetResp = () => request.BeginGetResponse(tar.AsSafe(gr =>
            {
                HttpWebResponse resp;
                Stream respStm;
                string contentType;
                try
                {
                    resp = (HttpWebResponse)request.EndGetResponse(gr);
                    contentType = ExtractContentType(resp);
                    respStm = resp.GetResponseStream();
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        resp = (HttpWebResponse)ex.Response;
                        contentType = ExtractContentType(resp);

                        if (contentType == "text/javascript")
                        {
                            respStm = resp.GetResponseStream();
                            goto ok;
                        }
                    }

                    throw TransportError(ex);
                }

                ok: var buf = new byte[4096];
                var ms = new MemoryStream(buf.Length);

                AsyncCallback cbr = null;
                cbr = tar.AsSafe(br =>
                {
                    int cnt = respStm.EndRead(br);
                    if (cnt == 0)
                    {
                        if (!tar.IsCompleted)
                        {
                            ((IDisposable)resp).Dispose();
                            ms.Seek(0, SeekOrigin.Begin);
                            tar.Complete(new ResponseData
                            {
                                Json = Encoding.UTF8.GetString(ms.ToArray()),
                                ContentType = contentType,
                            }, false);
                        }
                    }
                    else
                    {
                        ms.Write(buf, 0, cnt);
                        respStm.BeginRead(buf, 0, buf.Length, cbr, null);
                    }
                }, ex => ((IDisposable)resp).Dispose());

                respStm.BeginRead(buf, 0, buf.Length, cbr, null);
            }), null);

            try
            {
                if (httpVerb == HttpVerb.Post)
                {
                    string postData = EncodeDictionary(args);
                    byte[] postDataBytes = Encoding.UTF8.GetBytes(postData);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = postDataBytes.Length;

                    request.BeginGetRequestStream(tar.AsSafe(rar =>
                    {
                        Stream rqStm = request.EndGetRequestStream(rar);
                        rqStm.BeginWrite(postDataBytes, 0, postDataBytes.Length, tar.AsSafe(wr =>
                        {
                            rqStm.EndWrite(wr);
                            beginGetResp();
                        }), null);
                    }), null);
                }
                else
                {
                    beginGetResp();
                }
            }
            catch (Exception ex)
            {
                if (!tar.IsCompleted)
                    tar.Complete(true, ex);
            }

            return tar;
        }

        internal static ResponseData EndRequest(IAsyncResult ar)
        {
            return TypedAsyncResult<ResponseData>.End(ar, ExConverter);
        }

        private IAsyncResult BeginCall(string relativePath,
                        HttpVerb httpVerb,
                        Dictionary<string, string> args,
                        AsyncCallback cb, object state)
        {
            relativePath = (relativePath ?? String.Empty).TrimStart('/');
            string url = GetApiBaseUrl(relativePath) + relativePath;

            if (args == null)
                args = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(AccessToken))
                args["access_token"] = AccessToken;
            if (url.StartsWith("https://api"))
                args["format"] = "json";

            return BeginRequest(url, httpVerb, args, cb, state);
        }

        private JsonObject EndCall(IAsyncResult ar)
        {
            var obj = JsonObject.CreateFromString(EndRequest(ar).Json, Culture);
            if (obj.IsDictionary)
                ThrowIfError(obj);

            return obj;
        }

        /// <summary>
        /// Begins to makes a Facebook API GET request asynchronously.
        /// </summary>
        /// <param name="relativePath">The path for the call, e.g. /username</param>
        /// <param name="cb">A callback to call upon operation complete.</param>
        /// <param name="state">user state to pass to the callback.</param>
        /// <exception cref="ArgumentNullException"><paramref name="relativePath"/> is null.</exception>
        /// <exception cref="FacebookApiException">pre-request evaluation is failed.</exception>
        public IAsyncResult BeginGet([NotNull] string relativePath, [CanBeNull] AsyncCallback cb, [CanBeNull] object state)
        {
            if (relativePath == null)
                throw Nre("relativePath");
            return BeginGet(relativePath, null, cb, state);
        }

        /// <summary>
        /// Makes a Facebook API GET request asynchronously. If <paramref name="relativePath"/> is <c>null</c>, <paramref name="args"/> should contain <c>ids</c> or another facebook blessed approach to mean it is a batch call.
        /// </summary>
        /// <param name="relativePath">The path for the call, e.g. /username</param>
        /// <param name="args">A dictionary of key/value pairs that will get passed as query arguments.</param>
        /// <param name="cb">A callback to call upon operation complete.</param>
        /// <param name="state">user state to pass to the callback.</param>
        /// <exception cref="FacebookApiException">pre-request evaluation is failed.</exception>
        public IAsyncResult BeginGet([CanBeNull] string relativePath,
            [CanBeNull] Dictionary<string, string> args,
            [CanBeNull] AsyncCallback cb,
            [CanBeNull] object state)
        {
            return BeginCall(relativePath, HttpVerb.Get, args, cb, state);
        }

        /// <summary>
        /// Returns json status of the current operations which has been executing asynchronously.
        /// </summary>
        /// <param name="ar">The current operation async result.</param>
        /// <returns>json status of the operation.</returns>
        /// <exception cref="FacebookApiException">an exception occurred during the async call.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="ar"/> is null.</exception>
        public JsonObject EndGet(IAsyncResult ar)
        {
            return EndCall(ar);
        }

        /// <summary>
        /// Begins to make a Facebook API POST request asynchronously. If <paramref name="relativePath"/> is <c>null</c>, <paramref name="args"/> should contain <c>ids</c> or another facebook blessed approach to mean it is a batch call.
        /// </summary>
        /// <param name="relativePath">The path for the call, e.g. /username</param>
        /// <param name="args">A dictionary of key/value pairs that will get passed as query arguments. These determine what will get set in the graph API.</param>
        /// <param name="cb">A callback to call upon operation complete.</param>
        /// <param name="state">user state to pass to the callback.</param>
        /// <exception cref="FacebookApiException">pre-request evaluation is failed.</exception>
        public IAsyncResult BeginPost([CanBeNull] string relativePath,
            [CanBeNull] Dictionary<string, string> args,
            [CanBeNull] AsyncCallback cb,
            [CanBeNull] object state)
        {
            return BeginCall(relativePath, HttpVerb.Post, args, cb, state);
        }

        /// <summary>
        /// Returns json status of the current operations which has been executing asynchronously.
        /// </summary>
        /// <param name="ar">The current operation async result.</param>
        /// <returns>json status of the operation.</returns>
        /// <exception cref="FacebookApiException">an exception occurred during the async call.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="ar"/> is null.</exception>
        public JsonObject EndPost(IAsyncResult ar)
        {
            return EndCall(ar);
        }

        /// <summary>
        /// Begins to make a Facebook API DELETE request asynchronously.
        /// </summary>
        /// <param name="relativePath">The path for the call, e.g. /username.</param>
        /// <param name="cb">A callback to call upon operation complete.</param>
        /// <param name="state">user state to pass to the callback.</param>
        /// <exception cref="ArgumentNullException"><paramref name="relativePath"/> is null.</exception>
        /// <exception cref="FacebookApiException">pre-request evaluation is failed.</exception>
        public IAsyncResult BeginDelete([NotNull] string relativePath, [CanBeNull] AsyncCallback cb, [CanBeNull] object state)
        {
            if (relativePath == null)
                throw Nre("relativePath");
            return BeginCall(relativePath, HttpVerb.Delete, null, cb, state);
        }

        /// <summary>
        /// Begins to make a Facebook API DELETE request asynchronously. If <paramref name="relativePath"/> is <c>null</c>, <paramref name="args"/> should contain <c>ids</c> or another facebook blessed approach to mean it is a batch call.
        /// </summary>
        /// <param name="relativePath">The path for the call, e.g. /username</param>
        /// <param name="args">A dictionary of key/value pairs that will get passed as query arguments. These determine what will get set in the graph API.</param>
        /// <param name="cb">A callback to call upon operation complete.</param>
        /// <param name="state">user state to pass to the callback.</param>
        /// <exception cref="FacebookApiException">pre-request evaluation is failed.</exception>
        public IAsyncResult BeginDelete([CanBeNull] string relativePath,
            [CanBeNull] Dictionary<string, string> args,
            [CanBeNull] AsyncCallback cb,
            [CanBeNull] object state)
        {
            return BeginCall(relativePath, HttpVerb.Delete, args, cb, state);
        }

        /// <summary>
        /// Returns json status of the current operations which has been executing asynchronously.
        /// </summary>
        /// <param name="ar">The current operation async result.</param>
        /// <returns>json status of the operation.</returns>
        /// <exception cref="FacebookApiException">an exception occurred during the async call.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="ar"/> is null.</exception>
        public JsonObject EndDelete([NotNull] IAsyncResult ar)
        {
            return EndCall(ar);
        }
    }
}