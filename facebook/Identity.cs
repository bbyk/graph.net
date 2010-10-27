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
    /// Authentication primitive encapsulating <see cref="IAuthContext"/>.
    /// </summary>
    public class Identity : IIdentity
    {
        readonly IAuthContext _context;

        /// <summary>
        /// Initializes a new instance of <see cref="Identity"/> class with specified authentication context.
        /// </summary>
        /// <param name="context">The context to initialize with.</param>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
        public Identity([NotNull] IAuthContext context)
        {
            if (context == null)
                throw FacebookApi.Nre("context");
            _context = context;
        }

        /// <summary>
        /// Returns the current authentication context.
        /// </summary>
        [NotNull]
        public IAuthContext AuthContext { get { return _context; } }

        #region IIdentity Members

        /// <summary>
        /// Gets the type of authentication used. The value is <c>Forms</c> in case of the class.
        /// </summary>
        /// <value>Always <c>Forms</c></value>
        public string AuthenticationType
        {
            get { return "Forms"; }
        }

        /// <summary>
        /// Indicates whether the user has been authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get { return _context.IsAuthenticated; }
        }

        /// <summary>
        /// The name of the current user.
        /// </summary>
        public string Name
        {
            get { return _context.UserId.ToString(); }
        }

        #endregion
    }
}
