using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VisionCare.DataAccessLayer;
using VisionCare.BusinessLogicLayer;
using PayOS;

var builder = WebApplication.CreateBuilder(args);

PayOSClient payOS = new PayOSClient(
    builder.Configuration["PayOS:ClientId"] ?? throw new Exception("Cannot find environment variable: PayOS:ClientId"),
    builder.Configuration["PayOS:ApiKey"] ?? throw new Exception("Cannot find environment variable: PayOS:ApiKey"),
    builder.Configuration["PayOS:ChecksumKey"] ?? throw new Exception("Cannot find environment variable: PayOS:ChecksumKey")
);
builder.Services.AddSingleton(payOS);

// --- ĐĂNG KÝ SERVICES ---
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VisionCare API",
        Version = "v1"
    });

    // Thêm Bearer Token authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Name = "Authorization",
        In = ParameterLocation.Header,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Nhập Token (vd: Bearer {token})"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
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
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "VisionCare API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();