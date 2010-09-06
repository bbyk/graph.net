Graph.net - Facebook Graph API .NET Client
==========================================

This SDK provides a light wrapper around the Facebook Graph API with both **sync** and **async** calls.
Additionally it contains a set of useful canvas utilities to sustain authentication/authorization
workflows.

Forked from http://github.com/facebook/csharp-sdk.

NOTE: If you encounter any issues, please file them [here](http://github.com/bbyk/graph.net/issues).


Getting Started
---------------

The easiest way to get started is to have Visual Studio. You can get a free copy
of [Visual C# 2010 Express Edition from
Microsoft](http://www.microsoft.com/express/Downloads/#2010-Visual-CS).

The instructions that follow will assume that are you using this version. The
steps are very similar for other versions.

Once you have installed it and [downloaded the source of the
SDK](http://github.com/bbyk/graph.net/archives/master), you will need to
build the library. Open `FacebookAPI.sln`. Right click on
`FacebookAPI` in the **Solution Explorer** and choose **Build**. This shouldn't
take very long. The SDK is ready to use. You can either build on top of this
project, or use the DLL directly in another project.

There is a sample web app included. See the
`FacebookAPI.WebUI` project in the solution. Right click on the project and choose **Set as a StartUP Project**.
Now build the project, and run it. You will be taken through authentication/authorization process on Facebook. If you ok that,
the application shows your public profile information gathered with sync and **async
methods using application's and your access tokens.


Access Token
------------

Most data accessible via the [Graph
API](http://developers.facebook.com/docs/api) required an [access
token](http://developers.facebook.com/docs/authentication/). This SDK includes
a set of helpful methods of getting a token from users - see `CanvasUtil` and
[AuthenticationModule](http://github.com/bbyk/graph.net/blob/master/web/AuthenticationModule.cs) implementations.
Yet the best method will depend on what type of application is using it. A desktop application might show a
popup browser window that loads the Facebook site, for example. You can read
more about obtaining an access token [in the authentication
guide](http://developers.facebook.com/docs/authentication/).


Calling the Graph API
---------------------

First you instantiate an API object (passing in the token):

    Facebook.FacebookAPI api = new Facebook.FacebookAPI(token);

If you pass in `null` then you will only be able to access public data.

If you use the `CanvasUtil` and authentication primitives, it may look like:

    var identity = (Identity)Context.User.Identity;
    Facebook.FacebookAPI api = identity.Canvas.ApiClient;

Then you make calls like:

    JsonObject result = api.Get("/userid");

The same with **async** methods:

    JsonObject result = null;
    api.BeginGet("/userid", ar => result = api.EndGet(ar), null);

The `JsonObject` class provides a wrapper around JSON that allows for automatic
type conversion. In particular, it can treat JSON as a `Dictionary`, `Array`,
`String`, `Integer`, `Boolean` or `DateTime`. So to get the name of the userid as a string you would
do:

    string name = result.Dictionary["name"].String;

The SDK also supports `POST` and `DELETE` requests, for writing data. For
example to delete a comment once you have gotten its id you could do:

    api.Delete("/comment_id");

More useful is to use it with the async method as it doesn't block execution:

    api.BeginDelete("/comment_id", null, null);

To write a post on a user's wall you could do:

    var postArgs = new Dictionary<string, string> {
        { "message", "Hello, world!" }
    };
    api.BeginPost("/userid/feed", postArgs, null, null);

More information on the API itself can be found [in the developer
documentation](http://developers.facebook.com/docs/api).


Errors
------

Any errors in making Graph API calls cause a `FacebookAPIException` or a 'TimeoutException' to be
thrown.
