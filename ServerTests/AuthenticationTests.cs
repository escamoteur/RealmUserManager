using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RealmUserManagerDefinitions;
using Refit;

namespace ServerTests
{


    [TestClass]
    public class AuthenticationTests
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

    }
}
