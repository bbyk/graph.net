#region Facebook's boilerplate notice
/*
 * Copyright 2010 Facebook, Inc.
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

// THIS FILE IS MODIFIED SINCE FORKING FROM http://github.com/facebook/csharp-sdk/commit/52cf2493349494b783e321e0ea22335481b1c058 //

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

namespace Facebook
{
    ///<summary>
    ///</summary>
    public class FacebookApiException : Exception
    {
        ///<summary>
        ///</summary>
        public FacebookApiException() { }

        ///<summary>
        ///</summary>
        public string Type { get; set; }

        ///<summary>
        ///</summary>
        ///<param name="type"></param>
        ///<param name="msg"></param>
        public FacebookApiException(string type, string msg)
            : this(type, msg, null)
        {
        }

        ///<summary>
        ///</summary>
        ///<param name="type"></param>
        ///<param name="msg"></param>
        ///<param name="ex"></param>
        public FacebookApiException(string type, string msg, Exception ex)
            : base(msg, ex)
        {
            Type = type;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Type + ": " + base.ToString();
        }
    }
}
