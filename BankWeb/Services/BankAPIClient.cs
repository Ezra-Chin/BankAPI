using BankWeb.Models;
using Newtonsoft.Json;
using RestSharp;

namespace BankWeb.Services
{
    public partial class BankApiClient
    {
        private readonly RestClient restClient;

        public BankApiClient(IConfiguration configuration)
        {
            string baseUrl = configuration["BankApi:BaseUrl"]
                ?? throw new InvalidOperationException("BankApi:BaseUrl is missing from appsettings.json");

            var options = new RestClientOptions(baseUrl)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            restClient = new RestClient(options);
        }

        private async Task<ApiResult<T>> Send<T>(RestRequest restRequest)
        {
            var result = new ApiResult<T>();
            try
            {
                RestResponse restResponse = await restClient.ExecuteAsync(restRequest);
                result.StatusCode = (int)restResponse.StatusCode;

                if (restResponse.StatusCode == 0)
                {
                    result.ServiceUnavailable = true;
                    return result;
                }

                if (restResponse.IsSuccessful)
                {
                    result.Success = true;
                    if (!string.IsNullOrWhiteSpace(restResponse.Content))
                    {
                        result.Data = JsonConvert.DeserializeObject<T>(restResponse.Content);
                    }
                    return result;
                }

                result.Error = TryRead<ApiError>(restResponse.Content)
                               ?? new ApiError { Error = "HTTP_" + result.StatusCode, Message = "The bank service returned an error." };
                if (result.StatusCode >= 500)
                {
                    result.Error.Message = "The bank service had a problem. Please try again later.";
                }
                return result;
            }
            catch (Exception)
            {
                result.ServiceUnavailable = true;
                return result;
            }
        }

        private static TData? TryRead<TData>(string? json) where TData : class
        {
            try
            {
                return string.IsNullOrWhiteSpace(json) ? null : JsonConvert.DeserializeObject<TData>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}