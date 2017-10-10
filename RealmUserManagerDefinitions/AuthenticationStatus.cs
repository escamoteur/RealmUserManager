using System;
namespace RealmUserManagerDefinitions
{

    public class JwtToken
    {
        public Guid token; // Random ID that changes with each issuing of a AccessToken
        public string sub; // UserID
        public long exp;   // Expiry Date
    }


    // Content of a Login Request response
    public class AuthenticationStatus
    {
        public enum UserStatus { USER_VALID, USER_INACTIVE, USER_PAYMENT_EXPIRED,
            USER_IN_VALID
        }

        public DateTimeOffset EndOfSubscription { get; set; }

        public UserStatus Status;

        // Access token that has to be passed for every API call that needs authentication
        public string JwtToken { get; set; }

        //Will only be set if login was made using username / password
        public string RefreshToken { get; set; }
    }
}
