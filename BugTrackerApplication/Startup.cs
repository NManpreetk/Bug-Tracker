using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(BugTrackerApplication.Startup))]
namespace BugTrackerApplication
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
