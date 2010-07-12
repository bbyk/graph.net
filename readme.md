Facebook C# SDK
===============

This SDK provides a light wrapper around the Facebook Graph API.


Getting Started
---------------

The easiest way to get started is to have Visual Studio. You can get a free copy
of [Visual C# 2010 Express Edition from
Microsoft](http://www.microsoft.com/express/Downloads/#2010-Visual-CS).

The instructions that follow will assume that are you using this version. The
steps are very similar for other versions.

Once you have installed it and [downloaded the source of the
SDK](http://github.com/facebook/csharp-sdk/archives/master), you will need to
build the library. Open `facebook/FacebookAPI.csproj`. It may ask you to create
a solution, in which case you can just use the default name. Right click on
`FacebookAPI` in the **Solution Explorer** and choose **Build**. This shouldn't
take very long. The SDK is ready to use. You can either build on top of this
project, or use the DLL directly in another project.

There is a sample app included, which shows an example of the latter. Open
`examples/FacebookSampleApp.csproj`. Again, it may ask you to create a
solution. Go to the **Solution Explorer** and right click on **References**
and choose **Add a Reference**. Navigate to the DLL you built and select it.
Now build the project, and run it. You should see the string **Mark
Zuckerberg** printed out, if everything worked.


Access Token
------------

Most data accessible via the [Graph
API](http://developers.facebook.com/docs/api) required an [access
token](http://developers.facebook.com/docs/authentication/). This SDK does not
include a method of getting a token from a user, as the best method will depend
on what type of application is using it. A desktop application might show a
popup browser window that loads the Facebook site, for example. You can read
more about obtaining an access token [in the authentication
guide](http://developers.facebook.com/docs/authentication/).


Calling the Graph API
---------------------

First you instantiate an API object (passing in the token):

    Facebook.FacebookAPI api = new Facebook.FacebookAPI(token);

If you pass in `null` then you will only be able to access public data.
Then you make calls like:

    JSONObject result = api.Get("/userid");

The `JSONObject` class provides a wrapper around JSON that allows for automatic
type conversion. In particular, it can treat JSON as a `Dictionary`, `Array`,
`String`, or `Integer`. So to get the name of the userid as a string you would
do:

    string name = result.Dictionary["name"].String;

The SDK also supports `POST` and `DELETE` requests, for writing data. For
example to delete a comment once you have gotten its id you could do:

    api.Delete("/comment_id");

To write a post on a user's wall you could do:

    Dictionary<string, string> postArgs = new Dictionary<string, string>();
    postArgs["message"] = "Hello, world!";
    api.Post("/userid/feed", postArgs);

More information on the API itself can be found [in the developer
documentation](http://developers.facebook.com/docs/api).


Errors
------

Any errors in making Graph API calls cause a `FacebookAPIException` to be
thrown.
