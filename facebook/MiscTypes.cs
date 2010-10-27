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
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Globalization;

namespace Facebook
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "graph.net")]
    public class Session
    {
        static readonly DateTime s_unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// </summary>
        [DataMember(Name = "uid")]
        public long UserId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "access_token")]
        public string OAuthToken { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "expires")]
        public DateTime Expires { get; set; }

        ///<summary>
        ///</summary>
        [DataMember(Name = "sig")]
        public string Signature { get; set; }

        /// <summary>
        /// So called session secret.
        /// </summary>
        [DataMember(Name = "secret")]
        public string Secret { get; set; }

        /// <summary>
        /// Old session_key.
        /// </summary>
        [DataMember(Name = "session_key")]
        [Obsolete("Use OAuthToken instead.")]
        public string SessionKey { get; set; }

        /// <summary>
        /// </summary>
        public bool IsExpired { get { return DateTime.UtcNow > Expires; } }

        ///<summary>
        ///</summary>
        ///<returns></returns>
        public Dictionary<string, string> ToDictionary()
        {
            var expires = Expires == DateTime.MaxValue ? "0" : ((long)(Expires - s_unixStart).TotalSeconds).ToString(CultureInfo.InvariantCulture);

            var dict = new Dictionary<string, string>(6);
            if (UserId > default(long))
                dict.Add("uid", UserId.ToString(CultureInfo.InvariantCulture));
            dict.Add("access_token", OAuthToken);
            dict.Add("expires", expires);

            if (!String.IsNullOrEmpty(Secret))
                dict.Add("secret", Secret);
#pragma warning disable 612,618
            if (!String.IsNullOrEmpty(SessionKey))
                dict.Add("session_key", SessionKey);
#pragma warning restore 612,618
            if (!String.IsNullOrEmpty(Signature))
                dict.Add("sig", Signature);

            return dict;
        }

        ///<summary>
        ///</summary>
        ///<returns></returns>
        public JsonObject ToJsonObject()
        {
            return JsonObject.Create(ToDictionary(), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return new JavaScriptSerializer().Serialize(ToDictionary());
        }

        ///<summary>
        ///</summary>
        ///<param name="data"></param>
        ///<returns></returns>
        ///<exception cref="ArgumentNullException"></exception>
        public static Session FromJsonObject([NotNull] JsonObject data)
        {
            if (data == null)
                throw FacebookApi.Nre("data");

            var expires = data.Dictionary["expires"].Integer;

            return new Session
            {
                UserId = data.Dictionary["uid"].Integer,
                OAuthToken = data.Dictionary["access_token"].String,
                // if user granted 'offline_access' permission, the 'expires' value is 0.
                Expires = expires == 0 ? DateTime.MaxValue : s_unixStart.AddSeconds(expires),
                Signature = data.Dictionary.ContainsKey("sig") ? data.Dictionary["sig"].String : null,
                Secret = data.Dictionary.ContainsKey("secret") ? data.Dictionary["secret"].String : null,
#pragma warning disable 612,618
                SessionKey = data.Dictionary.ContainsKey("session_key") ? data.Dictionary["session_key"].String : null,
#pragma warning restore 612,618
            };
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IApplicationBindings
    {
        /// <summary>
        /// Application ID
        /// </summary>
        [NotNull]
        string AppId { get; }

        /// <summary>
        /// Application Secret
        /// </summary>
        [NotNull]
        string AppSecret { get; }

        /// <summary>
        /// E.g. http://localhost:24526/
        /// </summary>
        [NotNull]
        string SiteUrl { get; }

        /// <summary>
        /// E.g. http://apps.facebook.com/graphdotnet/
        /// </summary>
        [NotNull]
        string CanvasPage { get; }
    }

    ///<summary>
    ///</summary>
    public interface ISessionStorage
    {
        ///<summary>
        /// Determines if the storage is secure and the library does not need to verify the Session given.
        ///</summary>
        bool IsSecure { get; }

        ///<summary>
        /// Fetches from or saves to an instance of facebook <see cref="Facebook.Session"/> using an underlying storage.
        ///</summary>
        [CanBeNull]
        Session Session { get; set; }
    }

    /// <summary>
    /// Indicates that the value of marked element could never be null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class NotNullAttribute : Attribute
    {
    }

    /// <summary>
    /// Indicates that the value of marked element can be null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CanBeNull : Attribute
    {
    }

    /// <summary>
    /// </summary>
    public struct LoginParams
    {
        /// <summary>
        /// Comma separated list of requested extended perms, see <see href="http://developers.facebook.com/docs/authentication/permissions"/>.
        /// In case of OAuth will be converted to scope=..., in case of using login.php it will be req_perms=...
        /// </summary>
        public string ReqPerms { get; set; }

        ///<summary>
        ///</summary>
        public LoginDialogDisplay Display { get; set; }

        ///<summary>
        ///</summary>
        public string CancelUrl { get; set; }
    }

    ///<summary>
    ///</summary>
    public enum LoginDialogDisplay
    {
        ///<summary>
        ///</summary>
        NotSet,
        ///<summary>
        /// Full page (default)
        ///</summary>
        Page,
        ///<summary>
        /// Popup view
        ///</summary>
        Popup,

        ///<summary>
        ///</summary>
        Wap,

        /// <summary>
        /// </summary>
        Touch,
    }
}
