using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IAccountService
    {
        Task<T> UpsertAccountAsync<T>(UpsertAccountHttpRequest httpRequest, int? accountId) where T : class, new();
        Task<BaseResponse<UpsertAccountHttpResponse>> Import(List<UpsertAccountHttpRequest> httpRequest);

        Task<T> RemoveAccountAsync<T>(List<int> accountIds) where T : class, new();
        Task<T> OpenInBrowserAsync<T>(int accountId) where T : class, new();

        Task<BaseResponse<UserAccountsOverviewHttpResponse>> GetMyAccountsAsync(GetMyAccountsHttpRequest httpRequest);

        Task<BaseResponse<GetAllMyAccountsChatsHttpResponse>> GetMyChatsAsync();
    }
}
