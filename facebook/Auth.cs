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
