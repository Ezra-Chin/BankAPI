using BankApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//var connectionString = builder.Configuration.GetConnectionString("DBManager") ?? throw new InvalidOperationException("Connection string 'DBManager' not found.");

//Configure the Error response from error catching of modeles creation itself 
builder.Services.AddControllersWithViews()
    .ConfigureApiBehaviorOptions(options =>
    {
        //customize the response when error failed by field 
        options.InvalidModelStateResponseFactory = context =>
        {
            //collect the details of errors by field name 
            var details = context.ModelState
                .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
            //return 400 with teh format of the error 
            return new BadRequestObjectResult(
                new ApiError("VALIDATION_FAILED", "One or more fields failed validation.", details));
        };
    });
// Registed DBManger so it can be uesd in controler 
builder.Services.AddDbContext<DBManager>();

var app = builder.Build();


// Configure the HTTP request pipeline.
//soruce https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0

    //app.UseExceptionHandler("/Home/Error");


    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            //if encountering unexpected exception , return http 500 with the ApiError Standard  
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                new ApiError("INTERNAL_ERROR", "An unexpected error occurred."));
        });
   });


//create dbs and apply seed data automatically on first run
//ref source: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DBManager>();
    db.Database.Migrate();
}

//THIS ISI SO THAT FOR 404 IT WILL FOLLOW THE FORMAT NEEDED 
app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    await response.WriteAsJsonAsync(new ApiError( response.StatusCode == 404 ? "NOT_FOUND" : "HTTP_" + response.StatusCode , "The requested URL doesnt exists"));
});

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
