using BankWeb.Models;
using Newtonsoft.Json;
using RestSharp;

namespace BankWeb.Services
{

    //parent class 
    public partial class BankApiClient
    {
        //store rest sharp client used to send http req
        private readonly RestClient restClient;

        public BankApiClient(IConfiguration configuration)
        {
            //get bank api base url from appsettings.json (if no url just thorw excpetion) 
            string baseUrl = configuration["BankApi:BaseUrl"]
                ?? throw new InvalidOperationException("BankApi:BaseUrl is missing from appsettings.json");

            //configure the client based on the url , time out after 15 seconds 
            var options = new RestClientOptions(baseUrl)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            //create teh rest client 
            restClient = new RestClient(options);
        }

        // (t here measn the type of data expected from url) 
        private async Task<ApiResult<T>> Send<T>(RestRequest restRequest)
        {
            //create obj to store result 
            var result = new ApiResult<T>();
            try
            {
                //send http req and wait for respoonse 
                RestResponse restResponse = await restClient.ExecuteAsync(restRequest);
                //store status code in res 
                result.StatusCode = (int)restResponse.StatusCode;

                //if status code = 0 , then it means no vaslid res was recived 
                if (restResponse.StatusCode == 0)
                {
                    result.ServiceUnavailable = true;
                    return result;
                }
                //check if http req was successful 
                if (restResponse.IsSuccessful)
                {
                    //if yes , set the result.Success as true 
                    result.Success = true;
                    //convert json resopnse into expected data (T) for this case
                    if (!string.IsNullOrWhiteSpace(restResponse.Content))
                    {
                        result.Data = JsonConvert.DeserializeObject<T>(restResponse.Content);
                    }
                    return result;
                }
                //convert error res into ApiError to be stored inside result.Error
                result.Error = TryRead<ApiError>(restResponse.Content)
                               ?? new ApiError { Error = "HTTP_" + result.StatusCode, Message = "The bank service returned an error." };
                //replace server error message with general message 
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
           // (TData here is a placehodler for type(could be any type, it oculd also be null in this case)
           //The <TData> basically means i can call something like TryRead<Boolean> then the return class of TData will be boolean now 
           //basically converts the json string into the specific data type caleld 
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