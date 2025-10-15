using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly AuthenticationStateProvider authenticationStateProvider;
        public bool IsAuthenticated { get; set; }
        private CurrentUser? CurrentUser { get; set; }
        public CurrentUserService(AuthenticationStateProvider authenticationStateProvider)
        {
            this.authenticationStateProvider = authenticationStateProvider;
        }


        public async Task<CurrentUser?> GetCurrentUser()
        {
            var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
            IsAuthenticated = authState?.User.Identity?.IsAuthenticated != null;

            if (!IsAuthenticated)
            {
                return null;
            }

            var idClaim = authState?.User.FindFirst("Id")?.Value;

            if (idClaim is null)
            {
                return null;
            }

            CurrentUser = new CurrentUser
            {
                Id = int.Parse(idClaim),
                Name = authState?.User?.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                Email = authState?.User?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            };

            return CurrentUser;
        }

        public void MarkAsNull()
        {
            CurrentUser = null;
        }
    }
}
