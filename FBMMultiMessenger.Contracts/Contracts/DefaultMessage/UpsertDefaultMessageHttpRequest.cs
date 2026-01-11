using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.DefaultMessage
{
    public class UpsertDefaultMessageHttpRequest
    {
        [Required(ErrorMessage = "Please enter default message")]
        public string Message { get; set; } = null!;

        public List<int> SelectedAccounts { get; set; } = new List<int>();
    }

    public class UpsertDefaultMessageHttpResponse
    {

    }
}
