using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;

namespace FBMMultiMessenger.Services
{
    internal class AccountService : IAccountService
    {
        private readonly IBaseService _baseService;

        public AccountService(IBaseService baseService)
        {
            this._baseService=baseService;

        }
        public async Task<T> UpsertAccountAsync<T>(UpsertAccountHttpRequest httpRequest, int? accountId) where T : class, new()
        {
            var apiType = SD.ApiType.POST;
            var url = "account";

            if (accountId is not null)
            {
                apiType = SD.ApiType.PUT;
                url = $"account/{accountId}";
            }

            var request = new ApiRequest<UpsertAccountHttpRequest>()
            {
                ApiType = apiType,
                Url = url,
                Data = httpRequest
            };

            return await _baseService.SendAsync<UpsertAccountHttpRequest, T>(request);
        }

        public async Task<BaseResponse<UserAccountsOverviewHttpResponse>> GetMyAccountsAsync(GetMyAccountsHttpRequest httpRequest)
        {
            var request = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url =$"account/me?pageNo={httpRequest.PageNo}&pageSize={httpRequest.PageSize}&keyword={httpRequest.Keyword}",
                Data = null
            };
            return await _baseService.SendAsync<object, BaseResponse<UserAccountsOverviewHttpResponse>>(request);
        }

        public async Task<T> RemoveAccountAsync<T>(List<int> accountIds) where T : class, new()
        {
            var isMultipleDeleteRequest = accountIds.Count > 1;

            var url = isMultipleDeleteRequest ? "account/bulk" : $"account/{accountIds.FirstOrDefault()}";

            var data = isMultipleDeleteRequest ? accountIds : null;

            var request = new ApiRequest<List<int>>()
            {
                ApiType = SD.ApiType.DELETE,
                Url = url,
                Data = data
            };

            return await _baseService.SendAsync<List<int>, T>(request);
        }

        public async Task<T> GetMyChatsAsync<T>() where T : class, new()
        {
            var request = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url = "account/me/chats",
                Data = null
            };

            return await _baseService.SendAsync<object, T>(request);
        }

        public async Task<T> OpenInBrowserAsync<T>(int accountId) where T : class, new()
        {
            var request = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.POST,
                Url = $"account/{accountId}/open-in-browser",
                Data = null
            };

            return await _baseService.SendAsync<object, T>(request);
        }

        public async Task<BaseResponse<UpsertAccountHttpResponse>> Import(List<UpsertAccountHttpRequest> httpRequest)
        {
            var request = new ApiRequest<List<UpsertAccountHttpRequest>>()
            {
                ApiType = SD.ApiType.POST,
                Url = $"account/import",
                Data = httpRequest
            };

            return await _baseService.SendAsync<List<UpsertAccountHttpRequest>, BaseResponse<UpsertAccountHttpResponse>>(request);
        }
    }
}

