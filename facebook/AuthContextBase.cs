using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Facebook
{
    ///<summary>
    ///</summary>
    public abstract class AuthContextBase
    {
        ///<summary>
        ///</summary>
        public abstract string AppSecret { get; }

        CultureInfo _ci;
        /// <summary>
        /// </summary>
        static readonly Comparison<string> s_comparison = (s1, s2) => String.CompareOrdinal(s1, s2);

        /// <summary>
        /// </summary>
        public Action<Exception> ExProcessor { get; set; }

        ///<summary>
        ///</summary>
        public ISessionStorage SessionStorage { get; set; }

        ///<summary>
        ///</summary>
        public CultureInfo Culture
        {
            get { return _ci ?? CultureInfo.CurrentCulture; }
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
        /// </summary>
        protected static byte[] FromBase64String(string str)
        {
            str = str.Replace('-', '+').Replace('_', '/');
            int mod = str.Length % 4;
            str = str.PadRight(str.Length + (mod == 0 ? 0 : 4 - mod), '=');
            return Convert.FromBase64String(str);
        }

        /// <summary>
        /// </summary>
        protected string GenerateSignature(JsonObject data)
        {
            var keys = new List<string>(data.Dictionary.Keys.Where(k => k != "sig"));
            keys.Sort(s_comparison);

            var sb = new StringBuilder();
            foreach (string key in keys)
                sb.Append(key).Append('=').Append(data.Dictionary[key].String);

            sb.Append(AppSecret);

            using (HashAlgorithm md5 = MD5.Create())
                return ByteArrayToHexString(md5.ComputeHash(Encoding.ASCII.GetBytes(sb.ToString())));
        }
    }
}