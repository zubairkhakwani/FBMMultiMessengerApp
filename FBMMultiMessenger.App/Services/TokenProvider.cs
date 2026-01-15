using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;
using Microsoft.JSInterop;

namespace FBMMultiMessenger.Services
{
    public class TokenProvider : ITokenProvider
    {
        private readonly IJSRuntime JS;

        public TokenProvider(IJSRuntime jSRuntime)
        {
            this.JS=jSRuntime;
        }
        public async Task<string?> GetTokenAsync()
        {
            return await JS.InvokeAsync<string>("myInterop.getItem", SD.AccessToken);
        }

        public async Task SetTokenAsync(string token)
        {
            await JS.InvokeVoidAsync("myInterop.setItem", SD.AccessToken, token);
        }
        public async Task RemoveTokenAsync()
        {
            await JS.InvokeVoidAsync("myInterop.removeItem", SD.AccessToken);
        }
    }
}
