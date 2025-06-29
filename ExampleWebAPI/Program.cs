using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using CellPhoneContactsAPI.Constants;
using CellPhoneContactsAPI.Services;
internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(opts =>
        {
            var title = "Our version api";
            var terms = new Uri("http://localhost:234/terms");
            var license = new OpenApiLicense()
            {
                Name="This is my full license information or a link to it."
            };
            var contact = new OpenApiContact()
            {
                Name = "somename",
                Email="e",
                Url=new Uri("http://localhost/")
            };

            opts.SwaggerDoc("v1", new OpenApiInfo()
            {
                Version = "v1",
                Title = $"{title} v1",
                Description = "",
                TermsOfService = terms,
                License = license,
                Contact = contact
            });
            opts.SwaggerDoc("v2", new OpenApiInfo()
            {
                Version = "v2",
                Title = $"{title} v2",
                Description = "",
                TermsOfService = terms,
                License = license,
                Contact = contact
            });
        });
        builder.Services.AddAuthorization(opts =>
        {
            opts.AddPolicy(PolicyConstants.MustHaveEmployeeId, policy => {
                policy.RequireClaim("employeeId");
            });
            opts.AddPolicy(PolicyConstants.MustBeOwner, policy => {
                // can add multiple things in this claim.
                //policy.RequireUserName("mberryh");
                //policy.RequireClaim("employeeId");

                policy.RequireClaim("title", "Business Owner");
            });
            opts.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser() // bare minimum is user is authenticated
                .Build();
        });
        builder.Services.AddAuthentication("Bearer")
            .AddJwtBearer(opts =>
            {

                string appSalt = builder.Configuration["Authentication:AppSalt"];

                opts.TokenValidationParameters = new()
                {
                    ValidateIssuer = true, // Enable issuer validation

                    ValidateAudience = true,
                    ValidIssuer = builder.Configuration.GetValue<string>("Authentication:Issuer"), // Expected issuer
                    ValidAudience = builder.Configuration.GetValue<string>("Authentication:Audience"),




                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    IssuerSigningKey = new SymmetricSecurityKey(
                        SHA256.Create().ComputeHash(
                            Encoding.ASCII.GetBytes(appSalt +
                            builder.Configuration.GetValue<string>("Authentication:SecretKey")!)))
                };
            });
        var apiVersioningBuilder = builder.Services.AddApiVersioning(opts =>
        {
            opts.AssumeDefaultVersionWhenUnspecified = true;
            opts.DefaultApiVersion = new(2, 0);
            opts.ReportApiVersions = true;
            
        });

        apiVersioningBuilder.AddApiExplorer(opts => {
            opts.GroupNameFormat = "'v'VVV";
            opts.SubstituteApiVersionInUrl = true;

        });


        builder.Services.AddSingleton<AuthService>(); // AI SAID TO USE THIS

        builder.Services.AddHostedService<RefreshTokenCleanupService>(); //Background job runs every hour to clean up expired/revoked tokens automatically

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger((options => options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0));
            app.UseSwaggerUI(opts => {
                opts.SwaggerEndpoint("/swagger/v2/swagger.json", "v2");
                opts.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            });
        }


        //we can disable this when we get a proxy.
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}