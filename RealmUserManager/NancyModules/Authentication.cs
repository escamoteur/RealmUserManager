using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nancy;
using Nancy.ModelBinding;
using Realms;
using RealmUserManager.Model;

namespace RealmUserManager.NancyModules
{
    public class Authentication : NancyModule
    {


        public Authentication(UserManager userManager )
        {
        //public UserModule(IAppConfiguration appConfig)
        //{
        //    Get("/user/{id}", args => "Hello from Nancy running on CoreCLR");

        //    Post("/user", args =>
        //    {
        //        var realm = Realm.GetInstance();
        //        var user = new User();
        //        realm.Write(() =>
        //        {
        //            user.UserID = Guid.NewGuid().ToString();
        //            this.BindTo(user);
        //            realm.Add(user);
        //        });
        //        return user.UserID ;
        //    });

        //}


        // Async method lacks 'await' operators and will run synchronously
            Post("/auth/login", async (parameters, cp) =>
            {
                var credentials = this.BindAndValidate<UserCredentials>(new BindingConfig() {BodyOnly = true});
                if (!ModelValidationResult.IsValid)
                {
                    Log.Logger.Warning("Login with invalid credentials {requestbody}", this.Request.Body.AsString());

                    return new Response()
                    {
                        StatusCode = HttpStatusCode.UnprocessableEntity,
                        ReasonPhrase = "No valid Credentialobject in Request"
                    };
                }

                var authentication = await qmdRepository.LoginUser(credentials.UserName, credentials.Password);

                if (authentication.Status == AuthenticationStatus.UserStatus.USER_VALID)
                {
                    return Response.AsJson(authentication);
                }

                if (authentication.Status == AuthenticationStatus.UserStatus.USER_INACTIVE)
                {
                    return Response.AsJson(authentication, HttpStatusCode.Unauthorized);
                }

                if (authentication.Status == AuthenticationStatus.UserStatus.USER_PAYMENT_EXPIRED)
                {
                    Log.Logger.Information("Login with invalid credentials {requestbody}", this.Request.Body.AsString());
                    return Response.AsJson(authentication, HttpStatusCode.PaymentRequired);
                }

                Log.Logger.Information("Login failed {@user}", credentials);

                return new Response()
                {
                    StatusCode = HttpStatusCode.Forbidden,
                    ReasonPhrase = "username or password unknown"
                };

            });





            Post("/auth/new", async (parameters, cp) =>
            {
                var newUser = this.BindAndValidate<UserCredentials>(new BindingConfig() {BodyOnly = true});

                if (!ModelValidationResult.IsValid)
                {
                    Log.Logger.Warning("Add User with invalid credentials {requestbody}", this.Request.Body.AsString());
                    var resp = new Response()
                    {
                        StatusCode = HttpStatusCode.UnprocessableEntity,
                        ReasonPhrase = "No valid Credentialobject in Request"
                    };
                    return resp;
                }

                if (await qmdRepository.AddUser(newUser))
                {
                    Log.Logger.Information("New user Added: {@user}", newUser);

                    return HttpStatusCode.OK;
                }
                else
                {
                    return HttpStatusCode.Conflict;
                }

            });



            Patch("/auth/subscription/activate", async (parameters, cp) =>
            {
                var credentials = this.BindAndValidate<UserCredentials>(new BindingConfig() {BodyOnly = true});
                if (!ModelValidationResult.IsValid)
                {
                    return new Response()
                    {
                        StatusCode = HttpStatusCode.UnprocessableEntity,
                        ReasonPhrase = "No valid Credentialobject in Request"
                    };
                }

                var authentication = await qmdRepository.ActivateUser(credentials);

                if (authentication.Status == AuthenticationStatus.UserStatus.USER_IN_VALID)
                {
                    Log.Logger.Warning("Activate user with wrong credentials: {@user}", credentials);

                    return new Response()
                    {
                        StatusCode = HttpStatusCode.Forbidden,
                        ReasonPhrase = "username or password unknown"
                    };
                }

                Log.Logger.Information("User Activated {@user}", credentials);

                return Response.AsJson(authentication, HttpStatusCode.OK);

            });


            Get("/auth/subscription/activate/{id}", async (parameters, cp) =>
            {
                AuthenticationStatus authentication = await qmdRepository.ActivateUser(parameters.id);

                if (authentication.Status == AuthenticationStatus.UserStatus.USER_IN_VALID)
                {
                    Log.Logger.Warning("Activate user with unknown UserID: {id}", parameters.id);

                    return new Response()
                    {
                        StatusCode = HttpStatusCode.Forbidden,
                        ReasonPhrase = "Unkown User"
                    };
                }

                Log.Logger.Information("User Activated {@user}", parameters.id);
                Log.Logger.Verbose("lang:{l}", Request.Query.lang);

                switch ((string) Request.Query.lang)
                {
                    case "de":
                        return View["activation_confirmation_de.html"];
                    default:
                        return View["activation_confirmation_en.html"];
                }
            });

            Patch("auth/subscription/end", async (parameters, cp) =>
            {
                var credentials = this.BindAndValidate<UserCredentials>(new BindingConfig() {BodyOnly = true});
                if (!ModelValidationResult.IsValid)
                {
                    return new Response()
                    {
                        StatusCode = HttpStatusCode.UnprocessableEntity,
                        ReasonPhrase = "No valid Credentialobject in Request"
                    };
                }

                var authentication = await qmdRepository.UpdateUserSubscription(credentials);

                if (authentication.Status == AuthenticationStatus.UserStatus.USER_IN_VALID)
                {
                    return new Response()
                    {
                        StatusCode = HttpStatusCode.Forbidden,
                        ReasonPhrase = "username or password unknown"
                    };
                }

                return Response.AsJson(authentication, HttpStatusCode.OK);

            });


            Post("/auth/subscription/activate/sendemail", async (parameters, cp) =>
            {
                var credentials = this.BindAndValidate<UserCredentials>(new BindingConfig() {BodyOnly = true});
                if (!ModelValidationResult.IsValid)
                {
                    return new Response()
                    {
                        StatusCode = HttpStatusCode.UnprocessableEntity,
                        ReasonPhrase = "No valid Credentialobject in Request"
                    };
                }

                if (await qmdRepository.SendActivationEmail(credentials.UserName))
                {
                    return HttpStatusCode.OK;
                }
                else
                {
                    return HttpStatusCode.Forbidden;
                }
            });
        }


    }
}
