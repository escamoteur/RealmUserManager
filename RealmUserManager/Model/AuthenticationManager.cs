using System;
using System.Linq;
using System.Threading.Tasks;
using Jose;
using Realms;
using Realms.Exceptions;
using RealmUserManager.Helpers;
using RealmUserManagerDefinitions;
using Serilog;

namespace RealmUserManager.Model
{
    public class AuthenticationManager
    {
        public Realm TheRealm { get; }
        private readonly IAppConfiguration _config;

        public AuthenticationManager(IAppConfiguration config, Realm theRealm)
        {
            TheRealm = theRealm;
            _config = config;
        }


        //Although this isn't parts of authentication it will allow you to signal the app on login if a new Version is avaliable
        // and if you want to force an update depending on what you set in the config file
        public bool AppUpdateAvailable(string currentAppVersion)
        {
            return String.Compare(currentAppVersion, _config.AppSettings.LatestAppVersion, StringComparison.Ordinal) < 0;
        }

        public bool AppUpdateMandatory(string currentAppVersion)
        {
            return String.Compare(currentAppVersion, _config.AppSettings.ForceAppUpdateVersion, StringComparison.Ordinal) < 0;
        }
        




        public bool AddUser(UserData user)
        {
            try
            {
                TheRealm.Write(() =>
                {
                    user.Id = Guid.NewGuid().ToString();
                    user.active = false;

                    var hashAndSalt = HashHelper.GenerateSaltedSHA1(user.Password);

                    user.HashedAndSaltedPassword = hashAndSalt.hashedAndSalted;
                    user.SaltString = hashAndSalt.salt;

                    TheRealm.Add(user);
                });
                Log.Information("New user Added: {@user}", user);
                return true;

            }
            catch (RealmDuplicatePrimaryKeyValueException e)
            {
                Log.Information("Try to add user a second time: {@user}", user);
                return false;
            }
        }



        public AuthenticationStatus LoginUser(string userName, string pwd)
        {
            var userInDB = TheRealm.All<UserData>()
                        .FirstOrDefault(u => (  u.UserName == userName));
            

            var authenticationStatus = new AuthenticationStatus() { JwtToken = null, Status = AuthenticationStatus.UserStatus.USER_IN_VALID};

            if (userInDB == null || !HashHelper.VerifyAgainstSaltedHash(userInDB.HashedAndSaltedPassword, userInDB.SaltString, pwd))
            {
                authenticationStatus.Status = AuthenticationStatus.UserStatus.USER_IN_VALID;
                return authenticationStatus;
            }

            //Only issue a new refresh token if none is available
            if (string.IsNullOrWhiteSpace(userInDB.RefreshToken))
            {
                authenticationStatus.RefreshToken = Guid.NewGuid().ToString();

                TheRealm.Write(() =>
                {
                    userInDB.RefreshToken = authenticationStatus.RefreshToken;
                });
            }
            else
            {
                authenticationStatus.RefreshToken = userInDB.RefreshToken;
            }


            // We return an refresh token even if the user is not activated or the subscription has expired.
            // In this cases we will not return a new access token (JWT token)


            //First check if active
            if (!userInDB.active && !_config.AppSettings.IgnoreUserActiveCheck)
            {
                authenticationStatus.Status = AuthenticationStatus.UserStatus.USER_INACTIVE;
                return authenticationStatus;
            }
            //if active user should have a valid subscription
            if (userInDB.EndOfSubscription <= DateTimeOffset.UtcNow && !_config.AppSettings.IgnoreSubscriptionValidCheck)
            {
                authenticationStatus.Status = AuthenticationStatus.UserStatus.USER_PAYMENT_EXPIRED;
                authenticationStatus.EndOfSubscription =userInDB.EndOfSubscription;
                return authenticationStatus;
            }

            //everything fine
            authenticationStatus.Status = AuthenticationStatus.UserStatus.USER_VALID;

            var accessToken = new JwtToken()
            {
                token = Guid.NewGuid(),
                sub = userInDB.Id, 
                exp = DateTime.UtcNow.AddMinutes(5).ToBinary()
            };

            authenticationStatus.JwtToken = Jose.JWT.Encode(accessToken, _config.SecretKey, JwsAlgorithm.HS256);

            authenticationStatus.EndOfSubscription = userInDB.EndOfSubscription;

          
            return authenticationStatus;
        }


        public AuthenticationStatus RefreshLogin(string refreshToken)
        {
            var userInDB = TheRealm.All<UserData>()
                .FirstOrDefault(u => u.RefreshToken == refreshToken);

            var authenticationStatus = new AuthenticationStatus() { JwtToken = null, Status = AuthenticationStatus.UserStatus.USER_IN_VALID };


            if (userInDB == null)
            {
                authenticationStatus.Status = AuthenticationStatus.UserStatus.USER_IN_VALID;
                return authenticationStatus;
            }

            //First check if active
            if (!userInDB.active && !_config.AppSettings.IgnoreUserActiveCheck)
            {
                authenticationStatus.Status = AuthenticationStatus.UserStatus.USER_INACTIVE;
                return authenticationStatus;
            }
            //if active user should have a valid subscription
            if (userInDB.EndOfSubscription <= DateTimeOffset.UtcNow && !_config.AppSettings.IgnoreSubscriptionValidCheck)
            {
                authenticationStatus.Status = AuthenticationStatus.UserStatus.USER_PAYMENT_EXPIRED;
                authenticationStatus.EndOfSubscription = userInDB.EndOfSubscription;
                return authenticationStatus;
            }

            //everything fine
            authenticationStatus.Status = AuthenticationStatus.UserStatus.USER_VALID;

            var payLoad = new JwtToken()
            {
                token = Guid.NewGuid(),
                sub = userInDB.Id,
                exp = DateTime.UtcNow.AddMinutes(5).ToBinary()
            };

            authenticationStatus.JwtToken = Jose.JWT.Encode(payLoad, _config.SecretKey, JwsAlgorithm.HS256);

            authenticationStatus.EndOfSubscription = userInDB.EndOfSubscription;
            authenticationStatus.RefreshToken = refreshToken; // don't change the refresh token

            return authenticationStatus;
        }



        public AuthenticationStatus UpdateUserSubscription(string refreshToken, DateTimeOffset newExpiryDate)
        {
            var userInDB = TheRealm.All<UserData>()
                .FirstOrDefault(u => u.RefreshToken == refreshToken);

            TheRealm.Write(() =>
            {
                userInDB.EndOfSubscription = newExpiryDate;
            });

            return new AuthenticationStatus()
            {
                Status = AuthenticationStatus.UserStatus.USER_VALID,
                EndOfSubscription = newExpiryDate
            };
        }

        public bool ActivateUser(UserData userData)
        {

            var userInDB = TheRealm.All<UserData>()
                .FirstOrDefault(u => (u.UserName == userData.UserName));

            if (userInDB == null || !HashHelper.VerifyAgainstSaltedHash(userInDB.HashedAndSaltedPassword, userInDB.SaltString, userData.Password))
            {
                return false;
            }

            TheRealm.Write(() => userInDB.active = true);

            return true;
        }


        public bool ActivateUser(string activationToken)
        {
            var userInDB = TheRealm.All<UserData>()
                .FirstOrDefault(u => u.LastActivationToken == activationToken);

            if (userInDB == null)
            {
                return false;
            }

            TheRealm.Write(() =>
            {
                userInDB.active = true;
                userInDB.LastActivationToken = null;
            });

            return true;
        }


        public UserData UpdatePassword(object token, object password)
        {
            throw new NotImplementedException();
        }

        public bool UpdateLanguage(string credentialsUserName, object lang)
        {
            throw new NotImplementedException();
        }


        public async Task<bool> SendActivationEmail(string userName)
        {
            var activationManager = new EmailManager("EmailSettings.json", _config);

            var userToActivate =  TheRealm.All<UserData>()
                            .FirstOrDefault(user => user.UserName == userName);


            if (userToActivate != null)
            {
                // Generate activation token
                TheRealm.Write(() => userToActivate.LastActivationToken = Guid.NewGuid().ToString());

                await activationManager.SendActivationEmail(userToActivate);
                return true;
            }

            return false;


        }


        public async Task<bool> SendResetPasswordEmail(string userName)
        {
            var activationManager = new EmailManager("EmailSettings.json",_config);

            var userWithResetRequest = TheRealm.All<UserData>()
                .FirstOrDefault(user => user.UserName == userName);


            if (userWithResetRequest != null)
            {
                await activationManager.SendActivationEmail(userWithResetRequest.Email, userWithResetRequest.Id, userWithResetRequest.Language);
                return true;
            }

            return false;
        }

        public void DeleteTestUser()
        {
            var testUser = TheRealm.All<UserData>()
                .FirstOrDefault(user => user.UserName == "TestUser");

            if (testUser != null)
            {
                TheRealm.Remove(testUser);
            }
        }
    }
}
