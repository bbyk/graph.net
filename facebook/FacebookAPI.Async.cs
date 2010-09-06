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
            Uri url,
            HttpVerb httpVerb,
            Dictionary<string, string> args,
            AsyncCallback cb, object state)
        {
            if (args != null && args.Keys.Count > 0 && httpVerb == HttpVerb.Get)
                url = new Uri(url.AbsoluteUri + "?" + EncodeDictionary(args));

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
            var baseUrl = new Uri("https://graph.facebook.com");
            var url = new Uri(baseUrl, relativePath);
            if (args == null)
                args = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(AccessToken))
            {
                args["access_token"] = AccessToken;
            }

            return BeginRequest(url, httpVerb, args, cb, state);
        }

        private JsonObject EndCall(IAsyncResult ar)
        {
            var obj = JsonObject.CreateFromString(EndRequest(ar).Json, Culture);
            if (obj.IsDictionary && obj.Dictionary.ContainsKey("error"))
                throw GraphError(obj);

            return obj;
        }

        /// <summary>
        /// Makes a Facebook Graph API GET request.
        /// </summary>
        /// <param name="relativePath">The path for the call,
        /// e.g. /username</param>
        /// <param name="cb"></param>
        /// <param name="state"></param>
        /// <exception cref="FacebookApiException"></exception>
        public IAsyncResult BeginGet(string relativePath, AsyncCallback cb, object state)
        {
            return BeginGet(relativePath, null, cb, state);
        }

        /// <summary>
        /// Makes a Facebook Graph API GET request.
        /// </summary>
        /// <param name="relativePath">The path for the call,
        /// e.g. /username</param>
        /// <param name="args">A dictionary of key/value pairs that
        /// will get passed as query arguments.</param>
        /// <param name="cb"></param>
        /// <param name="state"></param>
        /// <exception cref="FacebookApiException"></exception>
        public IAsyncResult BeginGet(string relativePath,
            Dictionary<string, string> args,
            AsyncCallback cb, object state)
        {
            return BeginCall(relativePath, HttpVerb.Get, args, cb, state);
        }

        /// <exception cref="FacebookApiException"></exception>
        public JsonObject EndGet(IAsyncResult ar)
        {
            return EndCall(ar);
        }

        /// <summary>
        /// Makes a Facebook Graph API POST request.
        /// </summary>
        /// <param name="relativePath">The path for the call,
        /// e.g. /username</param>
        /// <param name="args">A dictionary of key/value pairs that
        /// will get passed as query arguments. These determine
        /// what will get set in the graph API.</param>
        /// <param name="cb"></param>
        /// <param name="state"></param>
        /// <exception cref="FacebookApiException"></exception>
        public IAsyncResult Post(string relativePath,
            Dictionary<string, string> args,
            AsyncCallback cb, object state)
        {
            return BeginCall(relativePath, HttpVerb.Post, args, cb, state);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        /// <exception cref="FacebookApiException"></exception>
        public JsonObject EndPost(IAsyncResult ar)
        {
            return EndCall(ar);
        }

        /// <summary>
        /// Makes a Facebook Graph API DELETE request.
        /// </summary>
        /// <param name="relativePath">The path for the call,
        /// e.g. /username</param>
        /// <param name="cb"></param>
        /// <param name="state"></param>
        /// <exception cref="FacebookApiException"></exception>
        public IAsyncResult BeginDelete(string relativePath, AsyncCallback cb, object state)
        {
            return BeginCall(relativePath, HttpVerb.Delete, null, cb, state);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        /// <exception cref="FacebookApiException"></exception>
        public JsonObject EndDelete(IAsyncResult ar)
        {
            return EndCall(ar);
        }
    }
}