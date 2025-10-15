using FBMMultiMessenger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services.IServices
{
    public interface ICurrentUserService
    {
        Task<CurrentUser> GetCurrentUser();
        bool IsAuthenticated { get; set; }
        void MarkAsNull();

    }
}
