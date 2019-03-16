using Microsoft.Owin;
using Owin;
using Microsoft.AspNet.SignalR;
using RealTimeProgressBar;

[assembly: OwinStartupAttribute(typeof(Portal.Startup))]
namespace Portal
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.MapSignalR();
        }
    }
}
