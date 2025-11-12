using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class RemoveAccountHttpRequest
    {
        public List<int> AccountIds { get; set; } = new List<int>();
    }

    public class RemoveAccountHttpResponse
    {
    }
}
