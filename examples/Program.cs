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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Facebook;

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

            JSONObject me = api.Get("/4");
            Console.WriteLine(me.Dictionary["name"].String);
        }
    }
}
