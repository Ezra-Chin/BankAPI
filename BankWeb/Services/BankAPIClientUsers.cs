using BankWeb.Models;
using RestSharp;

namespace BankWeb.Services
{
    public partial class BankAPIClientUsers
    {
        //register new customer by sending it to Part A
        public Task<ApiResult<UserResponse>> Register(UserCreateProfile userCreateProfile)
        {
            //create POST req to API customer registration endpoint
            RestRequest restRequest = new RestRequest("api/users", Method.Post);

            restRequest.AddParameter("Username", userCreateProfile.Username);
            restRequest.AddParameter("Name", userCreateProfile.Name);
            restRequest.AddParameter("Email", userCreateProfile.Email);
            restRequest.AddParameter("Address", userCreateProfile.Address);
            restRequest.AddParameter("Phone", userCreateProfile.Phone);
            restRequest.AddParameter("Password", userCreateProfile.Password);

            if (userCreateProfile.Picture != null && userCreateProfile.Picture.Length > 0)
            {
                restRequest.AddFile(
                    //name of the file field expected by Part A
                    "Picture",
                    () => userCreateProfile.Picture.OpenReadStream(),
                    userCreateProfile.Picture.FileName,
                    userCreateProfile.Picture.ContentType
                );
            }

            return Send<UserResponse>(restRequest);
        }

        //login customer by sending their login details to Part A
        public Task<ApiResult<UserResponse>> Login(UserLogin userLogin)
        {
            //create POST req to API login endpoint
            RestRequest restRequest = new RestRequest("api/users/login", Method.Post);

            restRequest.RequestFormat = DataFormat.Json;
            restRequest.AddBody(userLogin);

            return Send<UserResponse>(restRequest);
        }
    }
}