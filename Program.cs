using System.Security.Cryptography;
using System.Text;
using DotnetAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors((options) =>
{
    options.AddPolicy("DevCors", (corsBuilder) =>
    {
        // Our front end dev urls
        corsBuilder.WithOrigins("http://localhost:4200", "http://localhost:3000", "http://localhost:8000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
    options.AddPolicy("ProdCors", (corsBuilder) =>
    {
        corsBuilder.WithOrigins("https://myProductionSite.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddScoped<IUserRepository, UserRepository>();

// -------- AUTHENTICATION -------
SymmetricSecurityKey tokenKey = GetSymmetricSecurityTokenKey(builder.Configuration);
TokenValidationParameters tokenValidationParameters = new()
{
    // The Same Key used for encoding the token, will be used for validating the token
    IssuerSigningKey = tokenKey,
    ValidateIssuer = false,
    ValidateIssuerSigningKey = false,
    ValidateAudience = false,
};
string authScheme = JwtBearerDefaults.AuthenticationScheme; // e.g. Bearer YOUR_TOKEN
builder
    .Services
    .AddAuthentication(authScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = tokenValidationParameters;
    });
// -------- AUTHENTICATION -------

/// --- MAIN METHOD ---
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseCors("ProdCors");
    app.UseHttpsRedirection();
}
app.MapControllers();
// UseAuthentication() should always come before UseAuthorization, otherwise, u will encounter strange 
// error and 401 Unauthorize arror
app.UseAuthentication();
app.UseAuthorization();
app.Run();

static SymmetricSecurityKey GetSymmetricSecurityTokenKey(IConfiguration configuration)
{
    string tokenKeyFromAppSettings = configuration
          .GetSection("AppSettings:TokenKey")
          .Value!;
    byte[] tokenKeyFromAppSettingsInBytes = Encoding
        .UTF8
        .GetBytes(tokenKeyFromAppSettings);
    SymmetricSecurityKey tokenKey = new(tokenKeyFromAppSettingsInBytes);
    return tokenKey;
}
