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
using System.Security.Principal;

namespace Facebook
{
    /// <summary>
    /// </summary>
    public class Identity : IIdentity
    {
        [NonSerialized]
        readonly IAuthUtil _util;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="util"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Identity(IAuthUtil util)
        {
            if (util == null)
                throw new ArgumentNullException("util");
            _util = util;
        }

        /// <summary>
        /// 
        /// </summary>
        public IAuthUtil Auth { get { return _util; } }

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
