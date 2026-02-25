using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class GetAccountStatusHttpResponse
    {
        public List<AccountStatusResponse> Statuses { get; set; }
    }

    public class AccountStatusResponse
    {
        public int Id { get; set; }
        public bool IsConnected { get; set; }
    }
}
