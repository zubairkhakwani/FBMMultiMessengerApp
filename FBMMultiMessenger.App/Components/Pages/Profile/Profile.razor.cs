using FBMMultiMessenger.Contracts.Contracts.Profile;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FBMMultiMessenger.Components.Pages.Profile
{
    public partial class Profile
    {
        public EditProfileHttpRequest ProfileModel { get; set; } = new();

        public ChangePasswordHttpRequest PasswordModel { get; set; } = new();

        [Inject]
        private IProfileService ProfileService { get; set; }

        [Inject]
        private ISnackbar Snackbar { get; set; }


        private string OriginalName = string.Empty;
        private string OriginalEmail = string.Empty;
        private string OriginalPhoneNumber = string.Empty;
        private string SubscriptionStartedAt = string.Empty;
        private string SubscriptionExpiredAt = string.Empty;
        private string RemainingTimeText = string.Empty;
        private int RemainingDaysCount;



        private string JoinedAt = string.Empty;
        private bool ShowProfileLoader = false;
        private bool ShowPasswordLoader = false;

        private string Password = string.Empty;

        private void IsStrongPassword(ChangeEventArgs input)
        {
            Password = input?.Value?.ToString() ?? "";
        }

        protected override async Task OnInitializedAsync()
        {
            var response = await ProfileService.GetMyProfileAsync();
            if (response.IsSuccess)
            {
                var userData = response.Data ?? new GetMyProfleHttpResponse();
                var userName = userData.Name.Trim();
                var userEmail = userData.Email.Trim();
                var userPhoneNumber = userData.ContactNumber.Trim();

                //Profile related data
                ProfileModel.Name = userName;
                ProfileModel.Email = userEmail;
                ProfileModel.PhoneNumber = userPhoneNumber;

                //Subscription related data
                SubscriptionStartedAt = userData.StartedAt;
                SubscriptionExpiredAt = userData.ExpiredAt;
                RemainingTimeText = userData.RemainingTimeText;
                RemainingDaysCount = userData.RemainingDaysCount;

                CaptureProfileSnapshot(userName, userEmail, userPhoneNumber);


                JoinedAt = userData.JoinedAt.ToLocalTime().ToString("dd MMM yyyy");
                return;
            }

            var responseMessage = string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong while fetching your profile data." : response.Message;
            Snackbar.Add(responseMessage, Severity.Error);
        }

        private async Task OnValidProfileSubmit()
        {
            var hasProfileChange = HasProfileChange();
            if (!hasProfileChange)
            {
                Snackbar.Add("No changes deteced", Severity.Info);
                return;
            }

            ShowProfileLoader = true;

            var response = await ProfileService.EditProfileAsync(ProfileModel);

            if (response.IsSuccess)
            {
                CaptureProfileSnapshot(ProfileModel.Name, ProfileModel.Email, ProfileModel.PhoneNumber);
            }

            var responseMessage = string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong while updating your profile." : response.Message;
            Snackbar.Add(responseMessage, response.IsSuccess ? Severity.Success : Severity.Error);

            ShowProfileLoader = false;
        }

        private async Task OnValidPasswordSubmit()
        {
            var currentPassword = PasswordModel.CurrentPassword;
            var newPassword = PasswordModel.NewPassword;

            if (currentPassword == newPassword)
            {
                Snackbar.Add("New password cannot be the same as your current password.", Severity.Info);
                return;
            }


            ShowPasswordLoader = true;
            var response = await ProfileService.ChangePasswordAsync(PasswordModel);
            var responseMessage = string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong while updating your pasword." : response.Message;
            Snackbar.Add(responseMessage, response.IsSuccess ? Severity.Success : Severity.Error);
            ShowPasswordLoader = false;

        }

        private bool HasProfileChange()
        {
            if (OriginalName == ProfileModel.Name.Trim() && OriginalEmail == ProfileModel.Email.Trim() && OriginalPhoneNumber == ProfileModel.PhoneNumber.Trim())
            {
                return false;
            }
            return true;
        }
        private void CaptureProfileSnapshot(string name, string email, string phoneNumber)
        {
            OriginalName = name;
            OriginalEmail = email;
            OriginalPhoneNumber = phoneNumber;
        }


        private string GetProgressClass()
        {
            if (RemainingDaysCount < 0) return "Expired";
            if (RemainingDaysCount == 0) return "VeryCritical";
            if (RemainingDaysCount <= 3) return "Critical";
            if (RemainingDaysCount <= 7) return "VeryLow";
            if (RemainingDaysCount <= 14) return "Low";
            if (RemainingDaysCount <= 21) return "Medium";
            if (RemainingDaysCount < 30) return "Good";
            return "Excellent";
        }
    }
}
