using System;
using System.Runtime.Serialization;

namespace Facebook
{
    [Serializable]
    [DataContract(Namespace = "graph.net")]
    public class Session
    {
        [DataMember(Name = "user_id")]
        public long UserId { get; set; }

        [DataMember(Name = "oauth_token")]
        public string OAuthToken { get; set; }

        [DataMember(Name = "expires")]
        public DateTime Expires { get; set; }

        [DataMember(Name = "sig")]
        public string Signature { get; set; }

        public bool IsExpired { get { return DateTime.UtcNow > Expires; } }
    }

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
}
