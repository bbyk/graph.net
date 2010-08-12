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
using System.Linq;
using System.Text;
using Facebook;
using System.Globalization;

namespace FacebookSampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get an access token in some manner.
            // By default you can only get public info.
            string token = null;

            Facebook.FacebookAPI api = new Facebook.FacebookAPI(token);
            //api.Culture = new CultureInfo("en-us");

            JSONObject me = api.Get("/4");
            Console.WriteLine(me.Dictionary["name"].String + " " + me.Dictionary["gender"].String);
        }
    }
}
