
namespace FacebookAPI.WebUI
{
    public class AuthenticationModule : Facebook.AuthenticationModule
    {
        public override Facebook.CanvasUtil CreateCanvasUtil()
        {
            return new Facebook.CanvasUtil("119774131404605", "9f4233f1193a1affd3de35a305c06a0c");
        }
    }
}
