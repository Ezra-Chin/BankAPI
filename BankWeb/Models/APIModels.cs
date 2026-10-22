namespace BankWeb.Models
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class ApiError
    {
        public string Error { get; set; } = "";
        public string Message { get; set; } = "";
        public Dictionary<string, string[]>? Details { get; set; }
    }

    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public int StatusCode { get; set; }
        public bool ServiceUnavailable { get; set; }
        public ApiError? Error { get; set; }

        public string ErrorMessage => ServiceUnavailable
            ? "The bank service is currently unavailable. Please try again later."
            : Error?.Message ?? "Something went wrong. Please try again.";
    }
}