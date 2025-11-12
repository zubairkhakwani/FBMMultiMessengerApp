using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;


namespace FBMMultiMessenger.Components.Layout
{
    public partial class NavMenu
    {
        [Inject]
        public IAuthService AuthService { get; set; }

        public async Task Logout()
        {
            await AuthService.Logout();
        }
    }
}
