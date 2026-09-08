using FBMMultiMessenger.Components.Pages.Shared.CustomPopupform;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Color = MudBlazor.Color;

namespace FBMMultiMessenger.Components.Pages.ApiKeys
{
    public partial class ApiKeys
    {
        [Inject]
        private IApiKeyService ApiKeyService { get; set; }

        [Inject]
        private IDialogService DialogService { get; set; }

        [Inject]
        private ISnackbar Snackbar { get; set; }

        [Inject]
        private IJSRuntime JS { get; set; }

        private string ApiKeyValue = string.Empty;
        private string CreatedAt = string.Empty;
        private string UpdatedAt = string.Empty;

        private bool IsKeyActive;
        private bool IsKeyVisible;
        private bool IsLoading = true;
        private bool ShowActionLoader;

        private bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKeyValue);

        private string DisplayedKey => IsKeyVisible ? ApiKeyValue : MaskKey(ApiKeyValue);

        protected override async Task OnInitializedAsync()
        {
            await LoadApiKeyAsync();
        }

        private async Task LoadApiKeyAsync()
        {
            IsLoading = true;

            var response = await ApiKeyService.GetMyApiKeyAsync();

            IsLoading = false;

            if (!response.IsSuccess)
            {
                var responseMessage = string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong while fetching your API key." : response.Message;
                Snackbar.Add(responseMessage, Severity.Error);
                return;
            }

            if (response.Data is null)
            {
                ResetApiKey();
                return;
            }

            ApiKeyValue = response.Data.Key;
            IsKeyActive = response.Data.IsActive;
            CreatedAt = response.Data.CreatedAt.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt");
            UpdatedAt = response.Data.UpdatedAt.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt");
        }

        private async Task GenerateApiKeyAsync()
        {
            ShowActionLoader = true;

            var response = await ApiKeyService.GenerateApiKeyAsync();

            ShowActionLoader = false;

            var responseMessage = string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong while generating your API key." : response.Message;
            Snackbar.Add(responseMessage, response.IsSuccess ? Severity.Success : Severity.Error);

            if (!response.IsSuccess)
            {
                return;
            }

            // A freshly generated key is revealed so the user can copy it straight away.
            IsKeyVisible = true;
            await LoadApiKeyAsync();
        }

        private async Task RegenerateApiKeyAsync()
        {
            var parameters = new DialogParameters();
            parameters.Add("TitleText", "Regenerate API key");
            parameters.Add("ContentText", "Regenerating will permanently invalidate your current API key. Any browser extension or integration still using the old key will stop working until you update it.");
            parameters.Add("ButtonText", "Regenerate");
            parameters.Add("Color", Color.Primary);

            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };

            var result = await DialogService.Show<ConfirmationDialog>("", parameters, options).Result;

            if (result.Canceled)
            {
                return;
            }

            ShowActionLoader = true;

            var response = await ApiKeyService.RegenerateApiKeyAsync();

            ShowActionLoader = false;

            var responseMessage = string.IsNullOrWhiteSpace(response.Message) ? "Something went wrong while regenerating your API key." : response.Message;
            Snackbar.Add(responseMessage, response.IsSuccess ? Severity.Success : Severity.Error);

            if (!response.IsSuccess)
            {
                return;
            }

            IsKeyVisible = true;
            await LoadApiKeyAsync();
        }

        private async Task CopyApiKeyAsync()
        {
            bool isCopied = await JS.InvokeAsync<bool>("myInterop.copyToClipboard", ApiKeyValue);

            if (isCopied)
            {
                Snackbar.Add("API key copied to clipboard", Severity.Info);
                return;
            }

            Snackbar.Add("Failed to copy the API key", Severity.Error);
        }

        private void ToggleKeyVisibility()
        {
            IsKeyVisible = !IsKeyVisible;
        }

        private void ResetApiKey()
        {
            ApiKeyValue = string.Empty;
            CreatedAt = string.Empty;
            UpdatedAt = string.Empty;
            IsKeyActive = false;
            IsKeyVisible = false;
        }

        private static string MaskKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            // Reveal only enough of the key for the user to recognise it.
            const int visibleTrailingCharacters = 4;

            if (key.Length <= visibleTrailingCharacters)
            {
                return new string('•', key.Length);
            }

            return $"{new string('•', 24)}{key[^visibleTrailingCharacters..]}";
        }
    }
}
