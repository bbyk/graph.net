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

        /// <summary>
        /// </summary>
        public bool IsExpired { get { return DateTime.UtcNow > Expires; } }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            var expires = Expires == DateTime.MaxValue ? "0" : ((long)(Expires - s_unixStart).TotalSeconds).ToString(CultureInfo.InvariantCulture);

            return new JavaScriptSerializer().Serialize(new Dictionary<string, string>
            {
                { "uid", UserId.ToString(CultureInfo.InvariantCulture) },
                { "access_token", OAuthToken },
                { "expires", expires },
            });
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
        string AppId { get; }

        /// <summary>
        /// Application Secret
        /// </summary>
        string AppSecret { get; }

        /// <summary>
        /// E.g. http://localhost:24526/
        /// </summary>
        Uri SiteUrl { get; }

        /// <summary>
        /// E.g. http://apps.facebook.com/graphdotnet/
        /// </summary>
        Uri CanvasPage { get; }
    }

    /// <summary>
    /// Indicates that the value of marked element could never be null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class NotNullAttribute : Attribute
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
