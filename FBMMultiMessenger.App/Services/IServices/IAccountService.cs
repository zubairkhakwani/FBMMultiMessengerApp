using FBMMultiMessenger.Contracts.Contracts.Account;
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
        Task<T> RemoveAccountAsync<T>(int accountId) where T : class, new();
        Task<T> OpenInBrowserAsync<T>(int accountId) where T : class, new();

        Task<T> GetMyAccountsAsync<T>() where T : class, new();

        Task<T> GetMyChatsAsync<T>() where T : class, new();
    }
}
