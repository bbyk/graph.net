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
using System.Web;

namespace Facebook
{
    public class Auth
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FacebookAPIException"></exception>
        public FacebookAPI Authenticate()
        {
            string contentType;
            string result = FacebookAPI.MakeRequest(
                new Uri("https://graph.facebook.com/oauth/access_token"),
                HttpVerb.POST,
                null,
                new Dictionary<string, string> {
                    { "client_id", ClientId },
                    { "client_secret", ClientSecret },
                    { "type", "client_cred" }
                },
                out contentType);

            switch (contentType)
            {
                case "text/plain":
                    return new FacebookAPI { AccessToken = HttpUtility.ParseQueryString(result)["access_token"] };
                case "text/javascript":
                    var obj = JSONObject.CreateFromString(result);
                    if (obj.IsDictionary && obj.Dictionary.ContainsKey("error"))
                    {
                        throw FacebookAPI.ProtocolError(obj);
                    }

                    throw FacebookAPI.UnexpectedResponseError(result);
                default:
                    throw FacebookAPI.UnexpectedResponseError(result);
            }
        }
    }
}
