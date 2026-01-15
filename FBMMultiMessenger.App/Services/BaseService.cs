using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using static FBMMultiMessenger.Utility.SD;

namespace FBMMultiMessenger.Services
{
    internal class BaseService : IBaseService
    {
        public IHttpClientFactory httpClient { get; set; }
        private readonly ITokenProvider _tokenProvider;
        private readonly NavigationManager _navigationManager;
        private readonly string _baseUrl;

        public BaseService(IHttpClientFactory httpClientFactory, ITokenProvider tokenProvider, NavigationManager navigationManager, IConfiguration configuration)
        {
            httpClient = httpClientFactory;
            this._tokenProvider=tokenProvider;
            this._navigationManager=navigationManager;
            this._baseUrl = configuration.GetValue<string>("Urls:BaseUrl")!;

        }
        public async Task<TResponse> SendAsync<TRequest, TResponse>(ApiRequest<TRequest> apiRequest, bool withBearer = true, CancellationToken cancellationToken = default) where TRequest : class where TResponse : class, new()
        {
            try
            {
                var client = httpClient.CreateClient("MagicAPI");
                HttpRequestMessage message = new HttpRequestMessage();
                message.Headers.Add("Accept", "application/json");

                var url = $"{_baseUrl}/api/{apiRequest.Url}";
                message.RequestUri = new Uri(url);

                var token = await _tokenProvider.GetTokenAsync();
                if (token is not null && withBearer)
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                if (apiRequest.ApiType != SD.ApiType.GET)
                {
                    if (apiRequest.ContentType == ContentType.MultipartFormData)
                    {
                        message.Content = CreateMultipartContent(apiRequest);
                    }
                    else
                    {
                        message.Content = new StringContent(
                            JsonConvert.SerializeObject(apiRequest.Data),
                            Encoding.UTF8,
                            "application/json"
                        );
                    }
                }

                message.Method = apiRequest.ApiType switch
                {
                    SD.ApiType.POST => HttpMethod.Post,
                    SD.ApiType.PUT => HttpMethod.Put,
                    SD.ApiType.DELETE => HttpMethod.Delete,
                    _ => HttpMethod.Get
                };

                HttpResponseMessage responseMessage = await client.SendAsync(message, cancellationToken);
                var apiContent = await responseMessage.Content.ReadAsStringAsync();

                if (responseMessage.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await _tokenProvider.RemoveTokenAsync();
                    _navigationManager.NavigateTo("/login");
                }

                var APIResponse = JsonConvert.DeserializeObject<TResponse>(apiContent);
                return APIResponse ?? new TResponse();
            }
            catch (OperationCanceledException ex)
            {
                var data = BaseResponse<TResponse>.Error("Operation was cancelled");
                data.IsSuccess = true;
                data.APIRequestFailed = true;

                var res = JsonConvert.SerializeObject(data);
                var APIResponse = JsonConvert.DeserializeObject<TResponse>(res);
                return APIResponse ?? new TResponse();
            }


            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                var data = BaseResponse<TResponse>.Error("Something went wrong, please try later.");
                data.IsSuccess = false;
                data.APIRequestFailed = true;

                var res = JsonConvert.SerializeObject(data);
                var APIResponse = JsonConvert.DeserializeObject<TResponse>(res);
                return APIResponse ?? new TResponse();
            }
        }


        private MultipartFormDataContent CreateMultipartContent<TRequest>(ApiRequest<TRequest> apiRequest)
        where TRequest : class
        {
            var content = new MultipartFormDataContent();

            if (apiRequest.Data == null) return content;

            var properties = apiRequest.Data.GetType().GetProperties();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(apiRequest.Data);
                if (value == null) continue;

                // CASE 1: Single IBrowserFile property
                if (value is IBrowserFile singleFile)
                {
                    var stream = singleFile.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(singleFile.ContentType);
                    content.Add(streamContent, prop.Name, singleFile.Name);
                }

                // CASE 2: List<IBrowserFile> property
                else if (value is IEnumerable<IBrowserFile> fileList)
                {
                    foreach (var file in fileList)
                    {
                        var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
                        var streamContent = new StreamContent(stream);
                        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                        content.Add(streamContent, prop.Name, file.Name);
                    }
                }
                // CASE 3: Regular property (string, int)
                else
                {
                    if (value != null)
                    {
                        content.Add(new StringContent(value?.ToString() ?? string.Empty), prop.Name);
                    }
                }
            }

            return content;
        }
    }
}
