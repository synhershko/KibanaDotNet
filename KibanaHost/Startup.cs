using Microsoft.Owin.Security.ActiveDirectory;
using Owin;

namespace KibanaHost
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            if (!string.IsNullOrWhiteSpace(Config.Instance.AzureAdAudience) && !string.IsNullOrWhiteSpace(Config.Instance.AzureAdTenant))
                app.UseWindowsAzureActiveDirectoryBearerAuthentication(
                    new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                    {
                        Audience = Config.Instance.AzureAdAudience,
                        Tenant = Config.Instance.AzureAdTenant,
                    });

            app.UseNancy();
        }
    }
}
