namespace RealmUserManager
{
    public interface IAppConfiguration
    {
        Logging Logging { get; }
        Smtp Smtp { get; }
        ApplicationSettings AppSettings { get; }
        //Key used to encrypt the JWT Token
        byte[] SecretKey { get; set; }
    }
}