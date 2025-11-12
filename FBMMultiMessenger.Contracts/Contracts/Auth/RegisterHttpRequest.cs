using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Auth
{
    public class RegisterHttpRequest
    {
        [Required(ErrorMessage = "Please enter your name")]
        public string Name { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter your email")]
        [EmailAddress(ErrorMessage = "Please enter valid email address")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter password")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
         ErrorMessage = "Password must match all the requirements")]
        public string Password { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter confirm password")]
        [Compare(nameof(Password), ErrorMessage = "Password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter your contact number")]
        [RegularExpression(@"^(\+?\d{1,4}\s?)?\d{7,15}$",
        ErrorMessage = "Please enter a valid contact number")]
        public string ContactNumber { get; set; } = string.Empty;

        //[Required(ErrorMessage = "You must accept the terms")]
        //public bool AcceptTerms { get; set; }
    }

    public class RegisterHttpResponse
    {

    }
}
