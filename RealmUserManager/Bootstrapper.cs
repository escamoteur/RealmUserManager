
using System;
using System.Security.Claims;
using System.Security.Principal;
using Nancy;
using Nancy.Authentication.Stateless;
using Nancy.TinyIoc;
using Realms;
using RealmUserManager.Model;
using RealmUserManagerDefinitions;
using Serilog;

namespace RealmUserManager
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        private readonly IAppConfiguration appConfig;

        private AuthenticationManager _authenticationManager;

        public Bootstrapper()
        {
        }
        
        public Bootstrapper(IAppConfiguration appConfig)
        {
            this.appConfig = appConfig;
            _authenticationManager = new AuthenticationManager(appConfig, Realm.GetInstance());
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register<IAppConfiguration>(appConfig);
            container.Register<AuthenticationManager>(_authenticationManager);

            var authenticationConfiguration =
                new StatelessAuthenticationConfiguration(ctx =>
                {
                    var jwtToken = ctx.Request.Headers.Authorization;

                    try
                    {
                        var payload = Jose.JWT.Decode<JwtToken>(jwtToken, appConfig.SecretKey);

                        var tokenExpires = DateTime.FromBinary(payload.exp);

                        if (tokenExpires > DateTime.UtcNow)
                        {
                            return new ClaimsPrincipal(new GenericIdentity(payload.sub));
                        }

                        return null;


                    }
                    catch (Exception e)
                    {
                        Log.Logger.Warning("Access with invalid Access Token {token}", jwtToken);

                        return null;
                    }
                });

            container.Register<StatelessAuthenticationConfiguration>(authenticationConfiguration);

        }



    }   
}