using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.DefaultMessage
{
    public class UpsertDefaultMessageHttpRequest
    {
        [Required(ErrorMessage = "Please enter default message")]

        public string Message { get; set; } = null!;

        [Required]
        [MinLength(1, ErrorMessage = "Please select at least one account")]
        public List<int> SelectedAccounts { get; set; } = new List<int>();
    }

    public class UpsertDefaultMessageHttpResponse
    {

    }
}
