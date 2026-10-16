namespace BankApi.Data
{
    public class ApiError
    {
        public string Error { get; set; }
        public string Message { get; set; }
        public object Details { get; set; }

        public ApiError(string error, string message, object details = null)
        {
            Error = error;
            Message = message;
            Details = details; 
        }
    }
}
