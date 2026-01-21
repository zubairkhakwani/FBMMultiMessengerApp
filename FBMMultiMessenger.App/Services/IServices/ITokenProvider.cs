namespace FBMMultiMessenger.Services.IServices
{
    public interface ITokenProvider
    {
        Task SetTokenAsync(string token);
        Task<string?> GetTokenAsync();
        Task RemoveTokenAsync();
    }
}
