using Nancy;
using RealmUserManager.Model;

namespace RealmUserManager.NancyModules
{
    public class TestModule : NancyModule
    {
        public TestModule(AuthenticationManager authenticationManager)
        {

            // will delete a user with name "TestUser"
            Delete("/testing/testuser", parameters =>
            {
                if (Request.Query.testkey == "4711")
                {

                    authenticationManager.DeleteTestUser();
                    return 200;
                }
                else
                {
                    return HttpStatusCode.ExpectationFailed;
                }
            });


        }
    }
}