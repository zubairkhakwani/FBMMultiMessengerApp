using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var url = "api/account";

            if (accountId is not null)
            {
                apiType = SD.ApiType.PUT;
                url = $"api/account/{accountId}";
            }

            var request = new ApiRequest<UpsertAccountHttpRequest>()
            {
                ApiType = apiType,
                Url = url,
                Data = httpRequest
            };

            return await _baseService.SendAsync<UpsertAccountHttpRequest, T>(request);
        }

        public async Task<T> GetMyAccountsAsync<T>() where T : class, new()
        {
            var request = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url =$"account/me?pageNo={httpRequest.PageNo}&pageSize={httpRequest.PageSize}&keyword={httpRequest.Keyword}",
                Data = null
            };
            return await _baseService.SendAsync<object, T>(request);
        }

        public async Task<T> RemoveAccountAsync<T>(int accountId) where T : class, new()
        {
            var request = new ApiRequest<RemoveAccountHttpRequest>()
            {
                ApiType = SD.ApiType.PUT,
                Url = $"api/account/{accountId}/status",
                Data = null
            };

            return await _baseService.SendAsync<RemoveAccountHttpRequest, T>(request);
        }

        public async Task<T> GetMyChatsAsync<T>() where T : class, new()
        {
            var request = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url = "api/account/me/chats",
                Data = null
            };

            return await _baseService.SendAsync<object, T>(request);
        }

        public async Task<T> OpenInBrowserAsync<T>(int accountId) where T : class, new()
        {
            var request = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.POST,
                Url = $"api/account/{accountId}/open-in-browser",
                Data = null
            };

            return await _baseService.SendAsync<object, T>(request);
        }
    }
}

