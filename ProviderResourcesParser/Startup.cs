using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ProviderResourcesParser.Startup))]
namespace ProviderResourcesParser
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
