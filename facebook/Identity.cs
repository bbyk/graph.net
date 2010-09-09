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
        readonly IAuthContext _context;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Identity(IAuthContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            _context = context;
        }

        /// <summary>
        /// 
        /// </summary>
        public IAuthContext AuthContext { get { return _context; } }

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
            get { return _context.IsAuthenticated; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get { return _context.UserId.ToString(); }
        }

        #endregion
    }
}
