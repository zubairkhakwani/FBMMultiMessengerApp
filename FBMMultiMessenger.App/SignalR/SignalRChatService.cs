using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FBMMultiMessenger.SignalR
{
    public class SignalRChatService
    {
        private HubConnection _hubConnection;
        public event Func<HandleChatHttpResponse, Task> OnHandleMessage;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        private readonly string _baseURL;
        private bool _isReconnecting = false;
        private bool _shouldReconnect = true;
        private string userId;

        public SignalRChatService(IConfiguration configuration)
        {
            _baseURL = configuration.GetValue<string>("Urls:BaseUrl")!;
        }

        public async Task ConnectAsync(string userId)
        {
            this.userId = userId;
            _shouldReconnect = true;

            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{_baseURL}/chathub")
                    .Build();

                _hubConnection.On<HandleChatHttpResponse>("HandleMessage", async (messageData) =>
                {
                    if (OnHandleMessage != null)
                    {
                        await OnHandleMessage.Invoke(messageData);
                    }
                });

                await _hubConnection.StartAsync();
                await _hubConnection.SendAsync("RegisterUser", $"{userId}");

                _hubConnection.Closed += async (error) =>
                {
                    if (_shouldReconnect)
                    {
                        Console.WriteLine("SignalR disconnected, attempting to reconnect...");
                        await AttemptReconnect();
                    }
                };

            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong when connecting user to signalR");
            }
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
                    await Task.Delay(5000); // Wait 5 seconds

                    await _hubConnection.StartAsync();
                    await _hubConnection.SendAsync("RegisterUser", userId);

                    Console.WriteLine("Reconnected successfully!");
                    _isReconnecting = false;
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Reconnection failed: {ex.Message}, retrying in 5 seconds...");
                }
            }

            _isReconnecting = false;
        }

        public async Task HandleNotification(string deviceId, string fbChatId)
        {
            await _hubConnection.SendAsync("HandleNotification", deviceId, fbChatId);
        }

        public async Task DisconnectAsync()
        {
            _shouldReconnect = false;

            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
