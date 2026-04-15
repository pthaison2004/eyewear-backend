using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using VisionCare.DataAccessLayer;
using VisionCare.BusinessLogicLayer;


var builder = WebApplication.CreateBuilder(args);

// --- ĐĂNG KÝ SERVICES ---
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "VisionCare API";
        document.Info.Version = "v1";

        // 1. Khởi tạo an toàn (Phòng tránh lỗi NullReference)
        try 
        {
            document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
            if (document.Components.SecuritySchemes == null)
            {
                // Khởi tạo Dictionary dùng đúng Giao diện (IOpenApiSecurityScheme)
                document.Components.SecuritySchemes = new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
            }

            // 2. Định nghĩa Scheme
            var scheme = new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                Name = "Authorization",
                In = Microsoft.OpenApi.ParameterLocation.Header,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Nhập Token (vd: Bearer {token})"
            };

            document.Components.SecuritySchemes["Bearer"] = scheme;

            // 3. Tạo Requirement
            var requirement = new Microsoft.OpenApi.OpenApiSecurityRequirement();
            var schemeRef = new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document);
            
            requirement.Add(schemeRef, new List<string>());

            document.Security ??= new List<Microsoft.OpenApi.OpenApiSecurityRequirement>();
            document.Security.Add(requirement);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OpenAPI Error] {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        return Task.CompletedTask;
    });
});

// Cấu hình JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(secretKey)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddDataAccess(builder.Configuration);
builder.Services.AddBusinessLogic();

var app = builder.Build();

// --- CẤU HÌNH PIPELINE ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    
    // Sử dụng Swagger UI trỏ vào file JSON của OpenApi
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "VisionCare API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();