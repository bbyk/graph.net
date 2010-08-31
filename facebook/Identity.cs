using System;
using System.Security.Principal;

namespace Facebook
{
    public class Identity : IIdentity
    {
        CanvasUtil _util;

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

        public CanvasUtil Canvas { get { return _util; } }

        #region IIdentity Members

        public string AuthenticationType
        {
            get { return "Forms"; }
        }

        public bool IsAuthenticated
        {
            get { return _util.IsAuthenticated; }
        }

        public string Name
        {
            get { return _util.UserId.ToString(); }
        }

        #endregion
    }
}
