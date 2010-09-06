using System;
using System.Collections.Generic;

namespace Facebook
{
    ///<summary>
    ///</summary>
    public interface IAuthUtil
    {
        ///<summary>
        ///</summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// </summary>
        long UserId { get; }

        ///<summary>
        ///</summary>
        string AppId { get; }

        ///<summary>
        ///</summary>
        string AppSecret { get; }

        ///<summary>
        ///</summary>
        string AccessToken { get; }

        ///<summary>
        ///</summary>
        DateTime Expires { get; }

        ///<summary>
        ///</summary>
        FacebookApi ApiClient { get; }

        ///<summary>
        ///</summary>
        FacebookApi AppApiClient { get; }

        ///<summary>
        ///</summary>
        ///<param name="currentUrl"></param>
        ///<param name="params"></param>
        ///<returns></returns>
        string GetLoginUrl(Uri currentUrl, Dictionary<string, string> @params);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        string GetLogoutUrl(Uri currentUrl, Dictionary<string, string> @params);
    }
}
