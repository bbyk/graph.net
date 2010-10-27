using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Facebook
{
    ///<summary>
    /// Represents generic properties and methods of the authentication contexts used by Facebook: <see cref="CanvasAuthContext"/> and <see cref="OAuthContext"/>.
    ///</summary>
    public abstract class AuthContextBase : IFacebookApiFactory
    {
        CultureInfo _ci;
        TimeSpan? _timeout;
        IFacebookApiFactory _factory;
        static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(100);

        ///<summary>
        /// Application secret key.
        ///</summary>
        [NotNull]
        public abstract string AppSecret { get; }

        /// <summary>
        /// </summary>
        static readonly Comparison<string> s_comparison = (s1, s2) => String.CompareOrdinal(s1, s2);

        /// <summary>
        /// A callback to process unexpected exceptions when authenticating on behalf of the client code (e.g. logging).
        /// </summary>
        public Action<Exception> ExProcessor { get; set; }

        ///<summary>
        /// An instance of <see cref="ISessionStorage"/> class to store facebook session data in.
        ///</summary>
        public ISessionStorage SessionStorage { get; set; }

        ///<summary>
        /// Current locale for graph calls. Facebook graph is locale sensitive. If not provided, <see cref="CultureInfo.CurrentCulture"/> is used.
        ///</summary>
        public CultureInfo Culture
        {
            [NotNull]
            get { return _ci ?? CultureInfo.CurrentCulture; }
            [CanBeNull]
            set { _ci = value; }
        }

        static string ByteArrayToHexString(byte[] bytes)
        {
            var builder = new StringBuilder(3 * bytes.Length);
            foreach (byte t in bytes)
                builder.AppendFormat("{0:x2}", t);
            return builder.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// A helper to extract byte array from url encoded base64.
        /// </summary>
        protected static byte[] FromBase64String(string str)
        {
            str = str.Replace('-', '+').Replace('_', '/');
            int mod = str.Length % 4;
            str = str.PadRight(str.Length + (mod == 0 ? 0 : 4 - mod), '=');
            return Convert.FromBase64String(str);
        }

        /// <summary>
        /// A helper to generate MD5 signature for a <see cref="JsonObject" />. Returns <c>null</c> if <paramref name="data"/> is not a dictionary.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
        [CanBeNull]
        protected string GenerateSignature([NotNull] JsonObject data)
        {
            if (data == null)
                throw FacebookApi.Nre("data");

            if (!data.IsDictionary)
                return null;

            var keys = new List<string>(data.Dictionary.Keys.Where(k => k != "sig"));
            keys.Sort(s_comparison);

            var sb = new StringBuilder();
            foreach (string key in keys)
                sb.Append(key).Append('=').Append(data.Dictionary[key].String);

            sb.Append(AppSecret);

            using (HashAlgorithm md5 = MD5.Create())
                return ByteArrayToHexString(md5.ComputeHash(Encoding.ASCII.GetBytes(sb.ToString())));
        }

        ///<summary>
        /// See <see cref="IAuthContext.ApiClientFactory"/>.
        ///</summary>
        public IFacebookApiFactory ApiClientFactory
        {
            [NotNull]
            get { return _factory ?? this; }
            [CanBeNull]
            set { _factory = value; }
        }

        #region IFacebookApiFactory Members

        ///<summary>
        /// See <see cref="IFacebookApiFactory.Create"/>
        ///</summary>
        ///<returns></returns>
        FacebookApi IFacebookApiFactory.Create()
        {
            return CreateApiClient();
        }

        /// <summary>
        /// </summary>
        protected FacebookApi CreateApiClient()
        {
            return new FacebookApi { Proxy = Proxy, Timeout = Timeout, Culture = Culture, };
        }

        ///<summary>
        /// Gets or sets the proxy information for api requests.
        ///</summary>
        public IWebProxy Proxy { get; set; }

        ///<summary>
        /// Gets or sets timeout for api requests. Default timeout is 100 sec.
        ///</summary>
        ///<exception cref="ArgumentException">cannot be less than zero.</exception>
        public TimeSpan Timeout
        {
            get { return _timeout.HasValue ? _timeout.Value : s_defaultTimeout; }
            set { if (value <= TimeSpan.Zero) throw new ArgumentException("Timeout should be greater than zero.", "value"); _timeout = value; }
        }

        #endregion
    }
}