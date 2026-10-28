using System.Globalization;
using BankWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-AU");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-AU");

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddSingleton<BankApiClient>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "BankWeb.Auth";
        options.Cookie.HttpOnly = true;                
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        //See access deneid 
        options.AccessDeniedPath = "/Home/StatusCode/403";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        options.Events = new CookieAuthenticationEvents
        {
            //basically run this when the application tries to validates any cookies (context here contains the information of the cookie)
            OnValidatePrincipal = async context =>
            {
                // try to get username of logging in user from context (principal is who is the user and identity is how the user is identified)
                string? username = context.Principal?.Identity?.Name;
                //if null jsut skip it 
                if (username == null) return;
                //get the BankApiClient tool so the service can be used her e
                var api = context.HttpContext.RequestServices.GetRequiredService<BankApiClient>();
                //get user details  based on username 
                var result = await api.GetUser(username);
                //if doesnt exist 
                if (result.Success && result.Data != null && !result.Data.IsActive)
                {
                    //reject login cookie 
                    context.RejectPrincipal();
                    //sign user out by removing teh authenicated cookie 
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}");

app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store, no-cache";
    context.Response.Headers.Pragma = "no-cache";
    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();