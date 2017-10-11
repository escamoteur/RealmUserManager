using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Nancy.Owin;
using Serilog;

namespace RealmUserManager
{
    public class Startup
    {
        private readonly IConfiguration config;
        
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                              .AddJsonFile("appsettings.json")
                              .SetBasePath(env.ContentRootPath);

            config = builder.Build();
        }
        
        public void Configure(IApplicationBuilder app)
        {
            var appConfig = new AppConfiguration();
            ConfigurationBinder.Bind(config, appConfig);

            var log = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            app.UseOwin(x => x.UseNancy(opt => opt.Bootstrapper = new Bootstrapper(appConfig)));
        }
    }
}
