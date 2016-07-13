using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(samplemvc.Startup))]
namespace samplemvc
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
