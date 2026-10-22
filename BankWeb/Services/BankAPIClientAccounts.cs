using BankWeb.Models;
using RestSharp;

namespace BankWeb.Services
{
    public partial class BankApiClient
    {
        public Task<ApiResult<List<AccountModel>>> GetAccounts(string username)
        {
            RestRequest restRequest = new RestRequest("api/accounts", Method.Get);
            restRequest.AddQueryParameter("username", username);
            return Send<List<AccountModel>>(restRequest);
        }

        public Task<ApiResult<AccountModel>> GetAccount(string accountNumber)
        {
            RestRequest restRequest = new RestRequest("api/accounts/{accountNumber}", Method.Get);
            restRequest.AddUrlSegment("accountNumber", accountNumber);
            return Send<AccountModel>(restRequest);
        }

        public Task<ApiResult<AccountModel>> CreateAccount(string username, string accountName, decimal initialDeposit)
        {
            RestRequest restRequest = new RestRequest("api/accounts", Method.Post);
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.AddBody(new { Username = username, AccountName = accountName, InitialDeposit = initialDeposit });
            return Send<AccountModel>(restRequest);
        }

        public Task<ApiResult<PagedResult<TransactionModel>>> GetAccountHistory(TransactionFilter filter)
        {
            RestRequest restRequest = new RestRequest("api/accounts/{accountNumber}/transactions", Method.Get);
            restRequest.AddUrlSegment("accountNumber", filter.AccountNumber ?? "");
            if (filter.From != null) restRequest.AddQueryParameter("from", filter.From.Value.ToString("yyyy-MM-dd"));
            if (filter.To != null) restRequest.AddQueryParameter("to", filter.To.Value.ToString("yyyy-MM-dd"));
            restRequest.AddQueryParameter("page", filter.Page.ToString());
            restRequest.AddQueryParameter("pageSize", filter.PageSize.ToString());
            restRequest.AddQueryParameter("sortBy", filter.SortBy);
            restRequest.AddQueryParameter("sortDir", filter.SortDir);
            return Send<PagedResult<TransactionModel>>(restRequest);
        }

        public Task<ApiResult<object>> Transfer(string fromAccountNumber, string toAccountNumber, decimal amount, string? description)
        {
            RestRequest restRequest = new RestRequest("api/transactions/transfer", Method.Post);
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.AddBody(new
            {
                FromAccountNumber = fromAccountNumber,
                ToAccountNumber = toAccountNumber,
                Amount = amount,
                Description = description
            });
            return Send<object>(restRequest);
        }
    }
}