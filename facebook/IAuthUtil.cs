using System;
using System.Collections.Generic;

namespace Facebook
{
    ///<summary>
    ///</summary>
    public interface IAuthContext
    {
        ///<summary>
        ///</summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// </summary>
        /// <exception cref="NotSupportedException" />
        long UserId { get; }

        ///<summary>
        ///</summary>
        string AppId { get; }

        ///<summary>
        ///</summary>
        [NotNull]
        string AppSecret { get; }

        ///<summary>
        ///</summary>
        [NotNull]
        string AccessToken { get; }

        ///<summary>
        ///</summary>
        DateTime Expires { get; }

        ///<summary>
        ///</summary>
        [NotNull]
        FacebookApi ApiClient { get; }

        ///<summary>
        ///</summary>
        [NotNull]
        FacebookApi AppApiClient { get; }

        ///<summary>
        ///</summary>
        ///<param name="nextUrl"></param>
        ///<param name="params"></param>
        ///<returns></returns>
        ///<exception cref="ArgumentNullException" />
        string GetLoginUrl([NotNull] Uri nextUrl, Dictionary<string, string> @params);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        ///<exception cref="ArgumentNullException" />
        string GetLogoutUrl([NotNull] Uri nextUrl, Dictionary<string, string> @params);
    }
}
