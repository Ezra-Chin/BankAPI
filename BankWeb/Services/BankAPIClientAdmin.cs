using BankWeb.Models;
using RestSharp;

namespace BankWeb.Services
{
    public partial class BankApiClient
    {
        //get users by name, username or acc num 
        //GET api/users?search= 

        public Task<ApiResult<List<UserResponse>>> SearchUser(string? search)
        {
            // Create a Get request for /api/useres endpoint 
            RestRequest restRequest = new RestRequest("api/users", Method.Get);

            //add the search term if it is not empty 
            if (!string.IsNullOrWhiteSpace(search)) {
                //basically add ?search=search
                restRequest.AddQueryParameter("search", search.Trim());
            }

            // send the request and returns the response  (Send is sends the API , <List<serResponseo>> specifies what what response will be recived, restRequest is jsut the URL 
            return Send<List<UserResponse>>(restRequest);
        }


        //deactivate a user account
        //PATCH /api/users/{username}/deactivate
        public Task<ApiResult<object>> DeactivateUser(string Username)
        {
            //Create a request for /api/users/{username}/deactivate 
            RestRequest restRequest = new RestRequest("api/users/{username}/deactivate", Method.Patch);
            //shove Username insdie {username} 
            restRequest.AddUrlSegment("username", Username);
            //send the request and returns <object> 
            return Send<object>(restRequest);
        }


        //reactivate a user account
        //PATCH /api/users/{username}/reactivate
        public Task<ApiResult<object>> ReactivateUser(string Username)
        {
           
            RestRequest restRequest = new RestRequest("api/users/{username}/reactivate", Method.Patch);
            //shove Username insdie {username} 
            restRequest.AddUrlSegment("username", Username);
            //send the request and reutrns the <object> 
            return Send<object>(restRequest);
        }

        //get all transactiolsn 
        // GET api/transactions?from=&to=&search=&type=&opage=&pageSize=&sortBy=&sortOrder = 

        public Task<ApiResult<PagedResult<TransactionModel>>> GetAllTransactions(TransactionFilter filter)
        {
            //craete a request for /api/transcations 
            RestRequest restRequest = new RestRequest("api/transactions", Method.Get);
            //add ?from=from inside req 
            if (filter.From != null)
            {
                restRequest.AddQueryParameter("from", filter.From.Value.ToString("yyyy-MM-dd"));

            }
            //add &to=to insdie req 
            if (filter.To != null)
            {
                restRequest.AddQueryParameter("to", filter.To.Value.ToString("yyyy-MM-dd"));

            }
            //add &search=search insdie req 
            if (!string.IsNullOrEmpty(filter.Search))
            {
                restRequest.AddQueryParameter("search", filter.Search.Trim());
            }
            if (!string.IsNullOrEmpty(filter.Type))
            {
                restRequest.AddQueryParameter("type", filter.Type);
            }

            restRequest.AddQueryParameter("page", filter.Page.ToString());
            restRequest.AddQueryParameter("pageSize", filter.PageSize.ToString());
            restRequest.AddQueryParameter("sortBy", filter.SortBy);
            restRequest.AddQueryParameter("sortOrder", filter.SortDir);
            //send the request and returns <PagedResult<TransactionModel>> 
            return Send<PagedResult< TransactionModel >> (restRequest);

        }

        // Add an audit entry 
        // POST /api/audit 
        public Task<ApiResult<AuditEntry>> AddAudit(string adminusername, string action , string details)
        {
            //create a api request /api/audtion 
            RestRequest restRequest = new RestRequest("api/audit", Method.Post);
            //add a raw post req inside 
            restRequest.AddJsonBody(new { AdminUsername = adminusername, Action = action, Details = details });
            //send the request and expect the resposne to be auditEntry 
            return Send<AuditEntry>(restRequest);
        }



        //get audit entires
        //GET /api/audit?page=&pageSize=
        public Task<ApiResult<PagedResult<AuditEntry>>> GetAudit(int page = 1 , int pageSize = 10)
        {

            // create a api req for /api/audit 
            RestRequest restRequest = new RestRequest("api/audit", Method.Get);
            // add req for ?page=page 
            restRequest.AddQueryParameter("page", page.ToString());
            restRequest.AddQueryParameter("pageSize", pageSize.ToString());
            return Send<PagedResult<AuditEntry>>(restRequest);

        }

    }
}
