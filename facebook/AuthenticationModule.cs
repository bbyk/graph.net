using System;
using System.Web;
using System.Security.Principal;

namespace Facebook
{
    public abstract class AuthenticationModule : IHttpModule
    {
        public event EventHandler NoSession;

        #region IHttpModule Members

        public void Dispose()
        {
        }

        public void Init(HttpApplication app)
        {
            app.PostAcquireRequestState += (s, e) => OnEnter(app);
        }

        public abstract CanvasUtil CreateCanvasUtil();

        void OnEnter(HttpApplication app)
        {
            var util = CreateCanvasUtil();

            Session fbSession = util.GetSession(app.Context);
            if (fbSession == null)
            {
                EventHandler noSession = NoSession;
                if (noSession != null)
                    noSession(app, EventArgs.Empty);

                var url = util.GetLoginUrl(app.Request.Url);
                app.Context.Response.Write(String.Format(@"<script type=""text/javascript"">if (parent != self) top.location.href = ""{0}""; else self.location.href = ""{0}""</script>", url));
                app.CompleteRequest();
                return;
            }

            app.Context.User = new GenericPrincipal(new Identity(util), null);
        }

        #endregion
    }
}
