using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Components.Pages.Shared
{
    public partial class PasswordRequirement
    {
        private string ReqlengthClass = string.Empty;
        private string ReqUppercaseClass = string.Empty;
        private string ReqLowercaseClass = string.Empty;
        private string ReqNumberClass = string.Empty;
        private string ReqSpecialClass = string.Empty;


        [Parameter]
        public string Password { get; set; } = string.Empty;

        protected override void OnParametersSet()
        {
            IsStrongPassword();
        }
        private void IsStrongPassword()
        {
            var password = Password.ToString();

            bool hasValidLength = password.Length >= 8;
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasNumber = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            ReqlengthClass     = hasValidLength ? "valid" : string.Empty;
            ReqUppercaseClass  = hasUpper ? "valid" : string.Empty;
            ReqLowercaseClass  = hasLower ? "valid" : string.Empty;
            ReqNumberClass     = hasNumber ? "valid" : string.Empty;
            ReqSpecialClass    = hasSpecial ? "valid" : string.Empty;

            StateHasChanged();
        }
    }
}
