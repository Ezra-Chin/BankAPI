using BankWeb.Models;
using RestSharp;

namespace BankWeb.Services
{
    // restsharp calls to the part A web service for accounts, history and transfers
    public partial class BankApiClient
    {
        // GET api/accounts?username=bob
        // get all accounts of one user
        public Task<ApiResult<List<AccountModel>>> GetAccounts(string username)
        {
            RestRequest restRequest = new RestRequest("api/accounts", Method.Get);
            restRequest.AddQueryParameter("username", username);
            return Send<List<AccountModel>>(restRequest);
        }

        // GET api/accounts/{accountNumber}
        // get one account
        public Task<ApiResult<AccountModel>> GetAccount(string accountNumber)
        {
            RestRequest restRequest = new RestRequest("api/accounts/{accountNumber}", Method.Get);
            restRequest.AddUrlSegment("accountNumber", accountNumber);
            return Send<AccountModel>(restRequest);
        }

        // POST api/accounts
        // open a new account
        public Task<ApiResult<AccountModel>> CreateAccount(string username, string accountName, decimal initialDeposit)
        {
            RestRequest restRequest = new RestRequest("api/accounts", Method.Post);
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.AddBody(new { Username = username, AccountName = accountName, InitialDeposit = initialDeposit });
            return Send<AccountModel>(restRequest);
        }

        // GET api/accounts/{accountNumber}/transactions?from=&to=&page=&pageSize=&sortBy=&sortDir=
        // get one page of an account's history
        public Task<ApiResult<PagedResult<TransactionModel>>> GetAccountHistory(TransactionFilter filter)
        {
            RestRequest restRequest = new RestRequest("api/accounts/{accountNumber}/transactions", Method.Get);
            restRequest.AddUrlSegment("accountNumber", filter.AccountNumber ?? "");
            // only add the dates if the user picked them
            if (filter.From != null) restRequest.AddQueryParameter("from", filter.From.Value.ToString("yyyy-MM-dd"));
            if (filter.To != null) restRequest.AddQueryParameter("to", filter.To.Value.ToString("yyyy-MM-dd"));
            // paging and sorting
            restRequest.AddQueryParameter("page", filter.Page.ToString());
            restRequest.AddQueryParameter("pageSize", filter.PageSize.ToString());
            restRequest.AddQueryParameter("sortBy", filter.SortBy);
            restRequest.AddQueryParameter("sortDir", filter.SortDir);
            return Send<PagedResult<TransactionModel>>(restRequest);
        }

        // POST api/transactions/transfer
        // send money from one account to another
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