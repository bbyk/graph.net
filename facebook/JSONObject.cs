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
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Web.Script.Serialization;

namespace Facebook
{
    /// <summary>
    /// Represents an object encoded in JSON. Can be either a dictionary 
    /// mapping strings to other objects, an array of objects, or a single 
    /// object, which represents a scalar.
    /// </summary>
    public class JSONObject
    {
        long? _int;
        bool? _bool;

        /// <summary>
        /// Creates a JSONObject by parsing a string.
        /// This is the only correct way to create a JSONObject.
        /// </summary>
        public static JSONObject CreateFromString(string s)
        {
            object o;
            try
            {
                o = new JavaScriptSerializer().DeserializeObject(s);
            }
            catch (ArgumentException)
            {
                throw new FacebookAPIException("JSONException", "Not a valid JSON string.");
            }

            return Create(o);
        }

        /// <summary>
        /// Returns true if this JSONObject represents a dictionary.
        /// </summary>
        public bool IsDictionary
        {
            get
            {
                return _dictData != null;
            }
        }

        /// <summary>
        /// Returns true if this JSONObject represents an array.
        /// </summary>
        public bool IsArray
        {
            get
            {
                return _arrayData != null;
            }
        }

        /// <summary>
        /// Returns true if this JSONObject represents a string value. 
        /// </summary>
        public bool IsString
        {
            get
            {
                return _stringData != null;
            }
        }

        /// <summary>
        /// Returns true if this JSONObject represents an integer value.
        /// </summary>
        public bool IsInteger
        {
            get
            {
                long tmp;
                if (Int64.TryParse(_stringData, out tmp))
                {
                    _int = tmp;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns true if this JSONOBject represents a boolean value.
        /// </summary>
        public bool IsBoolean
        {
            get
            {
                bool tmp;
                if (bool.TryParse(ExtractQuoted(), out tmp))
                {
                    _bool = tmp;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns this JSONObject as a dictionary
        /// </summary>
        public Dictionary<string, JSONObject> Dictionary
        {
            get
            {
                return _dictData;
            }
        }

        /// <summary>
        /// Returns this JSONObject as an array
        /// </summary>
        public JSONObject[] Array
        {
            get
            {
                return _arrayData;
            }
        }

        /// <summary>
        /// Returns this JSONObject as a string
        /// </summary>
        public string String
        {
            get
            {
                return _stringData;
            }
        }

        /// <summary>
        /// Returns this JSONObject as an integer
        /// </summary>
        public Int64 Integer
        {
            get
            {
                return _int.HasValue ? _int.Value : Convert.ToInt64(_stringData, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Returns this JSONObject as a boolean
        /// </summary>
        public bool Boolean
        {
            get
            {
                return _bool.HasValue ? _bool.Value : Convert.ToBoolean(ExtractQuoted(), CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Prints the JSONObject as a formatted string, suitable for viewing.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            RecursiveObjectToString(this, sb, 0);
            return sb.ToString();
        }

        #region Private Members

        private string _stringData;
        private JSONObject[] _arrayData;
        private Dictionary<string, JSONObject> _dictData;

        private JSONObject() { }

        /// <summary>
        /// Recursively constructs this JSONObject 
        /// </summary>
        private static JSONObject Create(object o)
        {
            JSONObject obj = new JSONObject();

            object[] objArray;
            Dictionary<string, object> dict;
            if ((objArray = o as object[]) != null)
            {
                obj._arrayData = new JSONObject[objArray.Length];
                for (int i = 0; i < obj._arrayData.Length; ++i)
                {
                    obj._arrayData[i] = Create(objArray[i]);
                }
            }
            else if ((dict = o as Dictionary<string, object>) != null)
            {
                obj._dictData = new Dictionary<string, JSONObject>();
                foreach (string key in dict.Keys)
                {
                    obj._dictData[key] = Create(dict[key]);
                }
            }
            else if (o != null) // o is a scalar
            {
                obj._stringData = o.ToString();
            }

            return obj;
        }

        private static void RecursiveObjectToString(JSONObject obj,
            StringBuilder sb, int level)
        {
            if (obj.IsDictionary)
            {
                sb.AppendLine();
                RecursiveDictionaryToString(obj, sb, level + 1);
            }
            else if (obj.IsArray)
            {
                foreach (JSONObject o in obj.Array)
                {
                    RecursiveObjectToString(o, sb, level);
                    sb.AppendLine();
                }
            }
            else // some sort of scalar value
            {
                sb.Append(obj.String);
            }
        }
        private static void RecursiveDictionaryToString(JSONObject obj,
            StringBuilder sb, int level)
        {
            foreach (KeyValuePair<string, JSONObject> kvp in obj.Dictionary)
            {
                sb.Append(' ', level * 2);
                sb.Append(kvp.Key);
                sb.Append(" => ");
                RecursiveObjectToString(kvp.Value, sb, level);
                sb.AppendLine();
            }
        }

        string ExtractQuoted()
        {
            return _stringData != null
                && ((_stringData[0] == '\'' && _stringData[_stringData.Length - 1] == '\'')
                || (_stringData[0] == '"' && _stringData[_stringData.Length - 1] == '"')) ?
                _stringData.Substring(1, Math.Max(0, _stringData.Length - 2)) :
                _stringData;
        }


        #endregion

    }
}
