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
    public class UserModule : NancyModule
    {
        public UserModule(IAppConfiguration appConfig)
        {
            Get("/user/{id}", args => "Hello from Nancy running on CoreCLR");

            Post("/user", args =>
            {
                var realm = Realm.GetInstance();
                var user = new User();
                realm.Write(() =>
                {
                    user.UserID = Guid.NewGuid().ToString();
                    this.BindTo(user);
                    realm.Add(user);
                });
                return user.UserID ;
            });

        }

    }
}
