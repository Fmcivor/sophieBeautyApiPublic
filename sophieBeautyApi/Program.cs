using sophieBeautyApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.IdentityModel.Tokens;
using sophieBeautyApi.services;
using MongoDB.Driver;
// using Stripe;
using System.Text;
// using Azure.Identity;
// using DWQApi.AdditionalServices;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using sophieBeautyApi.Models;
using Microsoft.IdentityModel.Tokens;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

    var builder = WebApplication.CreateBuilder(args);

    // Azure Key Vault
    builder.Configuration.AddAzureKeyVault(
        new Uri("https://sophiebeautykeys.vault.azure.net/"),
        new ManagedIdentityCredential()
    );

    // Add services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddSingleton<MongoClient>(sp =>
    {
        var connectionString = builder.Configuration["mongoDB-conn"];
        return new MongoClient(connectionString);
    });

    builder.Services.AddScoped<treatmentService>();
    builder.Services.AddScoped<bookingService>();
    builder.Services.AddScoped<availablilitySlotService>();
    builder.Services.AddScoped<jwtTokenHandler>();
    builder.Services.AddScoped<adminService>();
    builder.Services.AddScoped<categoryService>();
    builder.Services.AddScoped<emailService>();

    // JWT Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = "https://sophiebeautyapi-c0hwdgf2hdbedfa5.ukwest-01.azurewebsites.net/",
                ValidIssuer = "https://sophiebeautyapi-c0hwdgf2hdbedfa5.ukwest-01.azurewebsites.net/",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["jwtSecret"]))
            };
        });

    // Swagger + JWT
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Your API", Version = "v1" });

        var jwtScheme = new OpenApiSecurityScheme
        {
            Scheme = "bearer",
            BearerFormat = "JWT",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Description = "Enter JWT Bearer token",
            Reference = new OpenApiReference
            {
                Id = JwtBearerDefaults.AuthenticationScheme,
                Type = ReferenceType.SecurityScheme
            }
        };

        c.AddSecurityDefinition(jwtScheme.Reference.Id, jwtScheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { jwtScheme, Array.Empty<string>() }
        });
    });

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("LocalAndFrontend", policy =>
            policy.WithOrigins(
                    "http://localhost:000",
                    "http://127.0.0.1:5500",
                    "http://192.168.1.71:5500",
                    "https://shapedbysophiee.netlify.app",
                    "https://www.shapedbysophiee.com" )
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());
    });

    var app = builder.Build();

    // Middleware
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }


    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors("LocalAndFrontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Run the app
    app.Run();
