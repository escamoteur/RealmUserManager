using System;
using System.Diagnostics;
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
        public RealmConfiguration _realmConfiguration { get; }
        private readonly IAppConfiguration _config;

        public AuthenticationManager(IAppConfiguration config, RealmConfiguration realmConfiguration)
        {
            _realmConfiguration = realmConfiguration;
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
            var theRealm = Realm.GetInstance(_realmConfiguration);
            try
            {
                theRealm.Write(() =>
                {
                    user.Id = Guid.NewGuid().ToString();
                    user.active = false;

                    var hashAndSalt = HashHelper.GenerateSaltedSHA1(user.Password);

                    user.HashedAndSaltedPassword = hashAndSalt.hashedAndSalted;
                    user.SaltString = hashAndSalt.salt;

                    theRealm.Add(user);
                });
                Log.Information("New user Added: {@user}", user);
                return true;

            }
            catch (RealmDuplicatePrimaryKeyValueException e)
            {
                Log.Information("Try to add user a second time: {@user}", user);
                return false;
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }
        }



        public AuthenticationStatus LoginUser(string userName, string pwd)
        {
            var theRealm = Realm.GetInstance(_realmConfiguration);

            var userInDB = theRealm.All<UserData>()
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

                theRealm.Write(() =>
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
            var theRealm = Realm.GetInstance(_realmConfiguration);

            var userInDB = theRealm.All<UserData>()
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
            var theRealm = Realm.GetInstance(_realmConfiguration);

            var userInDB = theRealm.All<UserData>()
                .FirstOrDefault(u => u.RefreshToken == refreshToken);

            theRealm.Write(() =>
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
            var theRealm = Realm.GetInstance(_realmConfiguration);

            var userInDB = theRealm.All<UserData>()
                .FirstOrDefault(u => (u.UserName == userData.UserName));

            if (userInDB == null || !HashHelper.VerifyAgainstSaltedHash(userInDB.HashedAndSaltedPassword, userInDB.SaltString, userData.Password))
            {
                return false;
            }

            theRealm.Write(() => userInDB.active = true);

            return true;
        }


        public bool ActivateUser(string activationToken)
        {
            var theRealm = Realm.GetInstance(_realmConfiguration);

            var userInDB = theRealm.All<UserData>()
                .FirstOrDefault(u => u.LastActivationToken == activationToken);

            if (userInDB == null)
            {
                return false;
            }

            theRealm.Write(() =>
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
            var theRealm = Realm.GetInstance(_realmConfiguration);

            var activationManager = new EmailManager("EmailSettings.json", _config);

            var userToActivate = theRealm.All<UserData>()
                            .FirstOrDefault(user => user.UserName == userName);


            if (userToActivate != null)
            {
                // Generate activation token
                theRealm.Write(() => userToActivate.LastActivationToken = Guid.NewGuid().ToString());

                await activationManager.SendActivationEmail(userToActivate);
                return true;
            }

            return false;


        }


        public async Task<bool> SendResetPasswordEmail(string userName)
        {
            var theRealm = Realm.GetInstance(_realmConfiguration);

            var activationManager = new EmailManager("EmailSettings.json",_config);

            var userWithResetRequest = theRealm.All<UserData>()
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
            try
            {
                var theRealm = Realm.GetInstance(_realmConfiguration);

                var testUser = theRealm.All<UserData>()
                    .FirstOrDefault(user => user.UserName == "TestUser");

                if (testUser != null)
                {
                    theRealm.Write(() => theRealm.Remove(testUser));
                }
            }
            catch (Exception e)
            {
                Debugger.Break();
                
            }
        }
    }
}
