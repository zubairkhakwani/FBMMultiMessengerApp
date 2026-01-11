using FBMMultiMessenger.Contracts.Contracts.DefaultMessage;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Models;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FBMMultiMessenger.Components.Pages.DefaultMessage
{
    public partial class UpsertDefaultMessage
    {
        public UpsertDefaultMessageHttpRequest model { get; set; } = new();
        public PopupFormSettings popupFormSettings { get; set; }

        [Parameter]
        public string? DefaultMessageId { get; set; }

        [Parameter]
        public string? DefaultMessage { get; set; }

        public List<DefaultMessageAccount> DefaultMessagesAccounts { get; set; } = new List<DefaultMessageAccount>();

        private IEnumerable<string> Options { get; set; } = new HashSet<string>();

        [Inject]
        public IDefaultMessageService DefaultMessageService { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [CascadingParameter]
        public IMudDialogInstance mudDialog { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }

        private string Title = "Add Default Message";
        private string SubTitle = "Create an automatic reply for your connected accounts";
        private string Heading = "New Default Message";
        private string Description = "Set up a message that will be automatically sent when someone messages your account.";
        private string ButtonText = "Add Message";


        protected override void OnInitialized()
        {
            if (!PlatformHelper.IsMobilePlatform)
            {
                popupFormSettings = new PopupFormSettings()
                {
                    Title = $"{(DefaultMessageId is null ? "Add" : "Edit")} Default Message",
                    Icon = Icons.Material.TwoTone.Markunread,
                    ContentHeight = "390px",
                    PopupView = PopupView.Single
                };
            }

            if (DefaultMessageId is not null)
            {
                Title = "Edit Default Message";
                SubTitle = "Modify your automated reply for selected accounts";
                Heading = "Edit Default Message";
                Description = "Update the message that will be sent automatically when someone messages your account.";
                ButtonText = "Edit Message";

                model.Message = DefaultMessage!;

                Options = DefaultMessageHelper.SelectableAccounts.SelectMany(x => new HashSet<string>()
                {
                    x.Id.ToString()

                }).ToHashSet();

                DefaultMessagesAccounts = DefaultMessageHelper.SelectableAccounts.Select(x => new DefaultMessageAccount()
                {
                    Id = x.Id,
                    Name = x.Name,

                }).ToList();

                var accountsThatCanNotBeUsed = DefaultMessageHelper.AccountsUsedForDefaultMessages.Select(x => new DefaultMessageAccount()
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsTaken = true
                });

                accountsThatCanNotBeUsed  = accountsThatCanNotBeUsed
                                            .Where(x => !DefaultMessageHelper.SelectableAccounts.Any(y => y.Id == x.Id));

                DefaultMessagesAccounts.AddRange(accountsThatCanNotBeUsed);

                var remainingAccounts = DefaultMessageHelper.AllAccounts
                                        .Where(x => !DefaultMessagesAccounts.Any(y => x.Id == y.Id))
                                        .Select(x => new DefaultMessageAccount()
                                        {
                                            Id = x.Id,
                                            Name = x.Name,
                                        });



                DefaultMessagesAccounts.AddRange(remainingAccounts);
            }

            else
            {
                //Add Request

                DefaultMessagesAccounts = DefaultMessageHelper.AccountsUsedForDefaultMessages.Select(x => new DefaultMessageAccount()
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsTaken = true
                }).ToList();

                var remainingAccounts = DefaultMessageHelper.AllAccounts
                                        .Where(x => !DefaultMessagesAccounts.Any(y => x.Id == y.Id))
                                        .Select(x => new DefaultMessageAccount()
                                        {
                                            Id = x.Id,
                                            Name = x.Name,
                                        });

                DefaultMessagesAccounts.AddRange(remainingAccounts);
            }
        }


        public async Task OnValidSubmit()
        {
            var IsValid = IsValidRequest();

            if (!IsValid) return;


            var selectedAccountIds = Options.SelectMany(x => new List<int>()
            {
                Convert.ToInt32(x)

            }).ToList();

            model.SelectedAccounts = selectedAccountIds;


            int? defaultMessageId = DefaultMessageId is not null ? Convert.ToInt32(DefaultMessageId) : null;
            var response = await DefaultMessageService.UpsertDefaultMessageAsync(model, defaultMessageId);

            if (response.IsSuccess)
            {
                Snackbar.Add(response.Message, Severity.Success);

                if (PlatformHelper.IsMobilePlatform)
                {
                    Navigation.NavigateTo($"/Default-Messages");
                }

                mudDialog?.Close(DialogResult.Ok(true));

                return;
            }

            Snackbar.Add(response.Message ?? "Something went wrong while adding default message, please try later.", Severity.Error);
        }



        public bool IsValidRequest()
        {
            if (string.IsNullOrWhiteSpace(model.Message))
            {
                Snackbar.Add("Please enter default message to continue", Severity.Info);
                return false;
            }

            if (DefaultMessageId is null &&  Options.Count() == 0)
            {
                Snackbar.Add("Please select at least one account to continue", Severity.Info);
                return false;
            }

            return true;
        }

        private string GetMultiSelectionText(List<string> selectedValues)
        {
            return $"{selectedValues.Count} account{(selectedValues.Count > 1 ? "s have" : " has")} been selected";
        }

        public void Cancel()
        {
            mudDialog.Cancel();
        }
    }
}
