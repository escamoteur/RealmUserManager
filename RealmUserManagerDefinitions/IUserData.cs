using System;

namespace RealmUserManagerDefinitions
{


    public interface IUserData
    {
        string Email { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
        DateTimeOffset EndOfSubscription { get; set; }
        bool active { get; }
        string Language { get; set; }
    }
}
