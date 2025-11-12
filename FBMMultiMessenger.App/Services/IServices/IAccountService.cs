using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Contracts.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IAccountService
    {
        Task<T> UpsertAccountAsync<T>(UpsertAccountHttpRequest httpRequest, int? accountId) where T : class, new();
        Task<BaseResponse<object>> Import(List<UpsertAccountHttpRequest> httpRequest);

        Task<T> RemoveAccountAsync<T>(List<int> accountIds) where T : class, new();
        Task<T> OpenInBrowserAsync<T>(int accountId) where T : class, new();

        Task<BaseResponse<PageableResponse<GetMyAccountsHttpResponse>>> GetMyAccountsAsync(GetMyAccountsHttpRequest httpRequest);

        Task<T> GetMyChatsAsync<T>() where T : class, new();
    }
}
