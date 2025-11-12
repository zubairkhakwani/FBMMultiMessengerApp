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

        [Required(ErrorMessage = "Please select any account")]
        public List<int> SelectedAccounts { get; set; } = null!;
    }

    public class UpsertDefaultMessageHttpResponse
    {

    }
}
