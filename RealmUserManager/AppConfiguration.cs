namespace RealmUserManager
{

    //This is just a sample App config
    public class AppConfiguration : IAppConfiguration
    {
        public Logging Logging { get; set; }
        public Smtp Smtp { get; set; }
        public ApplicationSettings AppSettings { get; set; }
        public string SecretKey { get; set; }
        public bool DebugMode { get; set; }
    }
    
    public class LogLevel
    {
        public string Default { get; set; }
        public string System { get; set; }
        public string Microsoft { get; set; }
    }

    public class Logging
    {
        public bool IncludeScopes { get; set; }
        public LogLevel LogLevel { get; set; }
    }

    // SMTP data settings for sending confirmation or passwort reset emails
    public class Smtp
    {
        public string Server { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public int Port { get; set; }
    }

    public class ApplicationSettings
    {
        public bool IgnoreUserActiveCheck { get; set; }  // Allows for testing to ignore the activation check
        public bool IgnoreSubscriptionValidCheck { get; set; } // Allows for testing to ignore subscription time

        public string LatestAppVersion { get; set; } // will be passed to the client when loggin in
        public string ForceAppUpdateVersion { get; set; } // will be passed to the client when loggin in so client can be notified that App should be updated
    }

}

