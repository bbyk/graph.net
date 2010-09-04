using System;
using System.Runtime.Serialization;

namespace Facebook
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "graph.net")]
    public class Session
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "user_id")]
        public long UserId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "oauth_token")]
        public string OAuthToken { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "expires")]
        public DateTime Expires { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "sig")]
        public string Signature { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsExpired { get { return DateTime.UtcNow > Expires; } }
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
}
