using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Realms;
using RealmUserManagerDefinitions;


namespace RealmUserManager.Model
{

    public class UserData : RealmObject, IUserData
    {
        // Unique ID that is part of the returned access tokens
		public string Id { get; set; }

        [PrimaryKey]
		public string Email { get; set; }
		public string UserName { get; set; }
        [Ignored]
        public string Password { get; set; } // this field is only used when a user logs in with username/password but is never saved 

		public string HashedAndSaltedPassword { get; set; }
        public string SaltString { get; set; }

        // these one time tokens are set when an reset password / activation email is send out 
        // and is cleared as soon as the associated link was clicked.
        // if another reset / activate was requested before the earlier one these tokens will be over written 
        public string RequestsPasswordResetToken { get; set; }
        public string LastActivationToken { get; set; }


        // Long living refresh token that is created when logging in with a user name / password
        // It can be used to request a new access token
        // It's safer to store this on a device than the real user credentials
        public string RefreshToken { get; set; }

        // only used if you want to use a subscription based user model
        public DateTimeOffset EndOfSubscription { get; set; }


        public bool active { get; internal set; }

        public string Language { get; set; }
	}
}
