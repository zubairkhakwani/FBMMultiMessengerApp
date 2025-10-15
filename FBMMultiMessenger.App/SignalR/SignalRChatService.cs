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

        public SignalRChatService(IConfiguration configuration)
        {
            _baseURL = configuration.GetValue<string>("Urls:BaseUrl")!;
        }

        public async Task ConnectAsync(string userId)
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{_baseURL}chathub")
                    .Build();

                _hubConnection.On<HandleChatHttpResponse>("HandleMessage", async (messageData) =>
                {
                    if (OnHandleMessage != null)
                    {
                        await OnHandleMessage.Invoke(messageData);
                    }
                });

                await _hubConnection.StartAsync();
                await _hubConnection.SendAsync("RegisterUser", $"App_{userId}");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong when connecting user to signalR");
            }
        }

        public async Task HandleNotification(string deviceId, string fbChatId)
        {
            await _hubConnection.SendAsync("HandleNotification", deviceId, fbChatId);
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
