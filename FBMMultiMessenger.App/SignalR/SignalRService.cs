using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Models.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using MudBlazor;

namespace FBMMultiMessenger.SignalR
{
    public class SignalRService
    {
        private HubConnection? _hubConnection;
        public event Func<HandleChatHttpResponse, Task> OnHandleMessage;
        public event Func<ChatInfoUpdatedSignalRModel, Task> OnHandleChatInfoUpdate;
        public event Func<List<AccountStatusSignalRModel>, Task> OnAccountStatusChange;
        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        private readonly string _baseURL;
        private bool _isReconnecting = false;
        private bool _shouldReconnect = true;
        private string userId;

        private Func<Exception?, Task>? _closedHandler;

        public SignalRService(IConfiguration configuration)
        {
            _baseURL = configuration.GetValue<string>("Urls:BaseUrl")!;
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
        }

        private void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                Console.WriteLine("Internet restored, reconnecting...");
            }
            else
            {
                Console.WriteLine("Internet lost, stopping connection...");
            }
        }

        public async Task ConnectAsync(string userId)
        {
            this.userId = userId;

            while (true)
            {
                try
                {
                    // Dispose existing connection if any
                    if (_hubConnection != null)
                    {
                        await DisconnectAsync();
                    }

                    _hubConnection = new HubConnectionBuilder()
                        .WithUrl($"{_baseURL}/chathub")
                        .Build();

                    RegisterEvents();

                    await _hubConnection.StartAsync();
                    
                    _shouldReconnect = true;

                    await _hubConnection.SendAsync("RegisterApp", $"{userId}");

                    //successfully connected, so breaking loop. if not connected it throw exception and while loop runs again.
                    break;
                }
                catch (OperationCanceledException ex)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await Task.Delay(500);
                }
            }
        }

        private void RegisterEvents()
        {
            _hubConnection.On<HandleChatHttpResponse>("HandleMessage", async (messageData) =>
            {
                if (OnHandleMessage != null)
                {
                    await OnHandleMessage.Invoke(messageData);
                }
            });

            _hubConnection.On<ChatInfoUpdatedSignalRModel>("HandleChatInfoUpdated", async (messageData) =>
            {
                if (OnHandleChatInfoUpdate != null)
                {
                    await OnHandleChatInfoUpdate.Invoke(messageData);
                }
            });

            _hubConnection.On<List<AccountStatusSignalRModel>>("HandleAccountStatus", async (accountsStatus) =>
            {
                if (OnAccountStatusChange != null)
                {
                    await OnAccountStatusChange.Invoke(accountsStatus);
                }
            });

            _closedHandler = async (error) =>
            {
                if (_shouldReconnect)
                {
                    await AttemptReconnect();
                }
            };

            _hubConnection.Closed += _closedHandler;

        }

        private async Task AttemptReconnect()
        {
            if (_isReconnecting || !_shouldReconnect) return;

            _isReconnecting = true;

            while (_hubConnection?.State != HubConnectionState.Connected && _shouldReconnect)
            {
                try
                {
                    Console.WriteLine("Reconnecting...");

                    await _hubConnection.StartAsync();
                    await _hubConnection.SendAsync("RegisterApp", userId);

                    Console.WriteLine("Reconnected successfully!");
                    _isReconnecting = false;
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Reconnection failed: {ex.Message}, retrying in 5 seconds...");
                    await Task.Delay(5000); // Wait 5 seconds
                }
            }

            _isReconnecting = false;
        }

        public async Task DisconnectAsync()
        {
            _shouldReconnect = false;

            if (_hubConnection != null)
            {
                if (_closedHandler != null)
                {
                    _hubConnection.Closed -= _closedHandler;
                }

                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
                await Task.Delay(500);
            }
        }
    }
}
