using System;
using System.Security.Principal;

namespace Facebook
{
    /// <summary>
    /// 
    /// </summary>
    public class Identity : IIdentity
    {
        readonly CanvasUtil _util;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="util"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Identity(CanvasUtil util)
        {
            if (util == null)
                throw new ArgumentNullException("util");
            _util = util;
        }

        /// <summary>
        /// 
        /// </summary>
        public CanvasUtil Canvas { get { return _util; } }

        #region IIdentity Members

        /// <summary>
        /// 
        /// </summary>
        public string AuthenticationType
        {
            get { return "Forms"; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsAuthenticated
        {
            get { return _util.IsAuthenticated; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get { return _util.UserId.ToString(); }
        }

        #endregion
    }
}
