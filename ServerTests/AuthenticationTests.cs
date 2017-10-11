using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RealmUserManagerDefinitions;
using Refit;
using MSTestExtensions;

namespace ServerTests
{


    [TestClass]
    public class AuthenticationTests : BaseTest
    {
        // Replace with your own serveradress
        private static string ServerBaseAdress = "http://192.168.178.51:5000/";



        [TestMethod]
        public async Task DeleteTestUser()
        {
            // We are using Refit to make the REST call
            var restAPI = RestService.For<IRealmUserManagerAPI>(ServerBaseAdress);

            await  restAPI.DeleteTestUser();
        }


        [TestMethod]
        public async Task AddUserTwice()
        {

            // We are using Refit to make the REST call
            var restAPI = RestService.For<IRealmUserManagerAPI>(ServerBaseAdress);
            //prepare
            await restAPI.DeleteTestUser();
            await restAPI.NewUser(new UserData()
            {
                Email = "thomas@burkharts.net",
                UserName = "TestUser",
                Password = "1234",
                EndOfSubscription = new DateTimeOffset(DateTime.Today.AddDays(10)),
                Language = "de"
            });


            Assert.ThrowsAsync<Refit.ApiException>(
            
                //act
                restAPI.NewUser(new UserData()
                {
                    Email = "thomas@burkharts.net",
                    UserName = "TestUser",
                    Password = "1234",
                    EndOfSubscription = new DateTimeOffset(DateTime.Today.AddDays(10)),
                    Language = "de"
                })
            );



        }

        [TestMethod]
        public async Task AddUser()
        {

            // We are using Refit to make the REST call
            var restAPI = RestService.For<IRealmUserManagerAPI>(ServerBaseAdress);

            //prepare
            await restAPI.DeleteTestUser();

            await Task.Delay(1000);

            await restAPI.NewUser(new UserData()
            {
                Email = "thomas@burkharts.net",
                UserName = "TestUser",
                Password = "1234",
                EndOfSubscription = new DateTimeOffset(DateTime.Today.AddDays(10)),
                Language = "de"
            });
        }

    }

    public class UserData : IUserData
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public DateTimeOffset EndOfSubscription { get; set; }
        public bool active { get; }
        public string Language { get; set; }
    }
}
