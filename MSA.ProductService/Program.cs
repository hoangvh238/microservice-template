using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MSA.Common.Contracts.Settings;
using MSA.Common.Mongo;
using MSA.Common.PostgresMassTransit.MassTransit;
using MSA.Common.Security.Authentication;
using MSA.Common.Security.Authorization;
using MSA.ProductService.Entities;

var builder = WebApplication.CreateBuilder(args);

// Register MongoDB and repositories
builder.Services.AddMongo()
                .AddRepositories<Product>("product")
                .AddMassTransitWithRabbitMQ()
                .AddMSAAuthentication()
                .AddMSAAuthorization(opt => {
                    opt.AddPolicy("read_access", policy => {
                        policy.RequireClaim("scope", "productapi.read");
                    });
                });

// Register controllers
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});

// Register Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();

var srvUrlsSetting = builder.Configuration
    .GetSection(nameof(ServiceUrlsSetting))
    .Get<ServiceUrlsSetting>();

builder.Services.AddSwaggerGen(options =>
{
    var scheme = new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{srvUrlsSetting.IdentityServiceUrl}/connect/authorize"),
                TokenUrl = new Uri($"{srvUrlsSetting.IdentityServiceUrl}/connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "productapi.read", "Access read operations" },
                    { "productapi.write", "Access write operations" }
                }
            }
        }
    };

    options.AddSecurityDefinition("OAuth", scheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "OAuth",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId("product-swagger");
        options.OAuthScopes("profile", "openid");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();