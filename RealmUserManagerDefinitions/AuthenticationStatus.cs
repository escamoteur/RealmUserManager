using System;
namespace RealmUserManagerDefinitions
{
    public class AuthenticationStatus
    {
        public enum UserStatus { USER_VALID, USER_INACTIVE, USER_PAYMENT_EXPIRED,
            USER_IN_VALID
        }

        public DateTime EndOfSubscription { get; set; }

        public UserStatus Status;

        public string JwtToken { get; set; }
    }
}
