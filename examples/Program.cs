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

            JSONObject me = api.Get("/me");
            Console.WriteLine(me.Dictionary["name"].String);
        }
    }
}
