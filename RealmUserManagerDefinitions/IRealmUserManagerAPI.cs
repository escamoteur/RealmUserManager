using System.Threading.Tasks;
using Refit;

namespace RealmUserManagerDefinitions
{
    public interface IRealmUserManagerAPI
    {
        [Post("/auth/login/")]
        Task<AuthenticationStatus> Login(IUserData credentials);

        [Post("/auth/user/new")]
        Task NewUser(IUserData credentials);

        [Patch("/auth/subscription/end")]
        Task<AuthenticationStatus> UpdateUserSubscriptionEnd(IUserData credentials);

        [Patch("/auth/subscription/activate")]
        Task<AuthenticationStatus> ActivateUser(IUserData credentials);

        [Post("/auth/subscription/activate/sendemail")]
        Task SendActivationEmail(IUserData credentials);

        // This will delete any user with name "TestUser"
        [Delete("/testing/testuser/?testkey=4711")]
        Task DeleteTestUser();


        [Get("/appUpdateAvailable?Version={version}")]
        Task<string> IsAppUpdateAvailable(string version);

        [Get("/appUpdateMandatory?Version={version}")]
        Task<string> IsAppUpdateMandatory(string version);
    }
}