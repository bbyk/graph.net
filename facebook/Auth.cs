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
using System.Web;

namespace Facebook
{
    /// <summary>
    /// 
    /// </summary>
    public class Auth
    {
        ///<summary>
        ///</summary>
        public string ClientId { get; set; }
        ///<summary>
        ///</summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FacebookApiException"></exception>
        public FacebookApi Authenticate()
        {
            string contentType;
            string result = FacebookApi.MakeRequest(
                new Uri("https://graph.facebook.com/oauth/access_token"),
                HttpVerb.Post,
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
                    return new FacebookApi { AccessToken = HttpUtility.ParseQueryString(result)["access_token"] };
                case "text/javascript":
                    var obj = JsonObject.CreateFromString(result, CultureInfo.InvariantCulture);
                    if (obj.IsDictionary && obj.Dictionary.ContainsKey("error"))
                    {
                        throw FacebookApi.GraphError(obj);
                    }

                    throw FacebookApi.UnexpectedResponse(result);
                default:
                    throw FacebookApi.UnexpectedResponse(result);
            }
        }
    }
}
