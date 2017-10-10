using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Extensions;
using Nancy.Security;
using Realms;
using RealmUserManager.Model;
using RealmUserManagerDefinitions;
using Serilog;

namespace RealmUserManager.NancyModules
{
    public class AuthenticationModule : NancyModule
    {


        public AuthenticationModule(AuthenticationManager authenticationManager)
        {


            // Async method lacks 'await' operators and will run synchronously
            Post("/auth/login", (parameters) =>
            {
                var credentials = this.BindAndValidate<UserData>(new BindingConfig() {BodyOnly = true});
                if (!ModelValidationResult.IsValid)
                {
                    Log.Logger.Warning("Login with invalid credentials {requestbody}", this.Request.Body.AsString());

                    return new Response()
                    {
                        StatusCode = HttpStatusCode.UnprocessableEntity,
                        ReasonPhrase = "No valid Credentialobject in Request"
                    };
                }

                var authentication = authenticationManager.LoginUser(credentials.UserName, credentials.Password);

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
                    Log.Logger.Information("Login expired subscription {requestbody}", this.Request.Body.AsString());
                    return Response.AsJson(authentication, HttpStatusCode.PaymentRequired);
                }

                Log.Logger.Information("Login failed {@user}", credentials);

                return new Response()
                {
                    StatusCode = HttpStatusCode.Forbidden,
                    ReasonPhrase = "username or password unknown"
                };

            });


            // Async method lacks 'await' operators and will run synchronously
            Post("/auth/refreshlogin", (parameters) =>
            {
                if (!this.Request.Query.token.HasValue)
                {
                    Log.Logger.Warning("Login with invalid refresh token", this.Request.Query.ToString());

                    return new Response()
                    {
                        StatusCode = HttpStatusCode.UnprocessableEntity,
                        ReasonPhrase = "No valid token in Request"
                    };
                }

                AuthenticationStatus authentication = authenticationManager.RefreshLogin(this.Request.Query.token);

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
                    Log.Logger.Information("Login expired subscription {refreshtoken}", this.Request.Query.token);
                    return Response.AsJson(authentication, HttpStatusCode.PaymentRequired);
                }

                Log.Logger.Information("Refresh Login failed {token}", this.Request.Query.token);

                return new Response()
                {
                    StatusCode = HttpStatusCode.Forbidden,
                    ReasonPhrase = "token invalid"
                };

            });


            Post("/auth/user/new", (parameters) =>
            {
                var newUser = this.BindAndValidate<UserData>(new BindingConfig() {BodyOnly = true});

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

                if (authenticationManager.AddUser(newUser))
                {
                    Log.Logger.Information("New user Added: {@user}", newUser);

                    return Task.FromResult(HttpStatusCode.OK);
                }
                else
                {
                    return HttpStatusCode.Conflict;
                }

            });



            // activate user by providing login credentials in the request body
            Patch("/auth/user/activate", (parameters) =>
            {
                var credentials = this.BindAndValidate<UserData>(new BindingConfig() {BodyOnly = true});
                if (!ModelValidationResult.IsValid)
                {
                    return new Response()
                    {
                        StatusCode = HttpStatusCode.UnprocessableEntity,
                        ReasonPhrase = "No valid Credentialobject in Request"
                    };
                }


                if (authenticationManager.ActivateUser(credentials))
                {

                    Log.Logger.Information("User Activated {@user}", credentials);


                    return HttpStatusCode.OK;
                }


                Log.Logger.Warning("Activate user with wrong credentials: {@user}", credentials);

                return new Response()
                {
                    StatusCode = HttpStatusCode.Forbidden,
                    ReasonPhrase = "username or password unknown"
                };

            });



            //activate user using a onetime token. As this route will be invoked through an activation link in a browser
            // we return a web page as result
            Patch("/auth/user/activate", parameters =>
            {

                if (authenticationManager.ActivateUser(Request.Query.token))
                {
                    Log.Logger.Information("User Activated {@user}", parameters.id);
                    Log.Logger.Verbose("lang:{l}", Request.Query.lang);

                    switch ((string) Request.Query.lang)
                    {
                        case "de":
                            return View["activation_confirmation_de.html"];
                        default:
                            return View["activation_confirmation_en.html"];
                    }
                }

                Log.Logger.Warning("Activate user with unknown token: {token}", parameters.token);

                switch ((string) Request.Query.lang)
                {
                    case "de":
                        return View["activation_failed_de.html"];
                    default:
                        return View["activation_failed_en.html"];
                }
            });


         //
            // Parameters:
            // token : a valid refresh token
            // newEndDate: UTC time as string
            Patch("auth/subscription/end",  parameters =>
            {
                if (!Request.Query.token.HasValue )
                {
                    return new Response()
                    {
                        StatusCode = HttpStatusCode.UnprocessableEntity,
                        ReasonPhrase = "No valid Credential object in Request"
                    };
                }


                if (!Request.Query.newEndDate.HasValue)
                {
                    return new Response()
                    {
                        StatusCode = HttpStatusCode.UnprocessableEntity,
                        ReasonPhrase = "No valid Credential object in Request"
                    };
                }

                DateTimeOffset newEndDate;
                try
                {
                    newEndDate = DateTimeOffset.Parse(!Request.Query.newEndDate);
                }
                catch (Exception e)
                {
                    return new Response()
                    {
                        StatusCode = HttpStatusCode.UnprocessableEntity,
                        ReasonPhrase = "Invalid date format"
                    };
                }

                AuthenticationStatus authentication = authenticationManager.UpdateUserSubscription(Request.Query.token, newEndDate);

                if (authentication.Status == AuthenticationStatus.UserStatus.USER_IN_VALID)
                {
                    return new Response()
                    {
                        StatusCode = HttpStatusCode.Forbidden,
                        ReasonPhrase = "user name or password unknown"
                    };
                }

                return Response.AsJson(authentication, HttpStatusCode.OK);

            });


            Post("/auth/sendActivationEmail", async (parameters, cp) =>
            {
                var credentials = this.BindAndValidate<UserData>(new BindingConfig() {BodyOnly = true});
                if (!ModelValidationResult.IsValid)
                {
                    return new Response()
                    {
                        StatusCode = HttpStatusCode.UnprocessableEntity,
                        ReasonPhrase = "No valid Credential object in Request"
                    };
                }

                if (await authenticationManager.SendActivationEmail(credentials.UserName))
                {
                    return HttpStatusCode.OK;
                }
                else
                {
                    return HttpStatusCode.Forbidden;
                }
            });

            Post("/auth/sendResetPassWord", async (parameters, cp) =>
            {
                await authenticationManager.SendResetPasswordEmail(Request.Query.userName);
                return HttpStatusCode.OK;
            });

            // updates the password of a user using a  one time token. As this route will be invoked through an link in an email 
            // a browser we return a web page as result
            Patch("/auth/user/pwd/", parameters =>
            {

                UserData user = authenticationManager.UpdatePassword(Request.Query.token, Request.Query.password);
                if (user != null)
                {
                    Log.Logger.Information("password change for {@user}", user);


                    switch ((string) Request.Query.lang)
                    {
                        case "de":
                            return View["change_password_confirmation_de.html"];
                        default:
                            return View["change_password_confirmation_en.html"];
                    }
                }

                Log.Logger.Warning("password change attempt user with invalid token: {token}", parameters.token);

                switch ((string) Request.Query.lang)
                {
                    case "de":
                        return View["change_password_failed_de.html"];
                    default:
                        return View["change_password_failed_en.html"];
                }
            });

            Patch("/auth/user/language", parameters =>
            {
                var credentials = this.BindAndValidate<UserData>(new BindingConfig() {BodyOnly = true});
                if (!ModelValidationResult.IsValid)
                {
                    return new Response()
                    {
                        StatusCode = HttpStatusCode.UnprocessableEntity,
                        ReasonPhrase = "No valid Credential object in Request"
                    };
                }

                if (authenticationManager.UpdateLanguage(credentials.UserName, parameters.lang))
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
