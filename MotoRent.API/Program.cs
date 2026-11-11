using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MotoRent.API.Middlewares;
using MotoRent.API.OData;
using MotoRent.API.Services;
using MotoRent.BusinessObjects.Common;
using MotoRent.DataAccess;
using MotoRent.Services;

var builder = WebApplication.CreateBuilder(args);

// File cấu hình riêng từng máy (không đưa lên Git) — đặt connection string SQL Server của bạn ở đây
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// ===== Database =====
builder.Services.AddDbContext<MotorbikeRentalContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== Repository + Service =====
builder.Services.AddMotoRentServices();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// ===== Controllers + OData =====
builder.Services
    .AddControllers()
    .AddOData(options => options
        .Select().Filter().OrderBy().Expand().Count().SetMaxTop(100)
        .AddRouteComponents("odata", EdmModelBuilder.Build()))
    .ConfigureApiBehaviorOptions(options =>
    {
        // Lỗi validate DTO → trả về ApiResponse thay vì ProblemDetails mặc định
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
            return new BadRequestObjectResult(ApiResponse<object>.Fail("Dữ liệu không hợp lệ.", errors));
        };
    });

// ===== JWT Authentication =====
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Chưa cấu hình Jwt:Key trong appsettings.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MotoRent.API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MotoRent.Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MotoRent API", Version = "v1" });
    // Ẩn các endpoint metadata của OData khỏi Swagger
    c.DocInclusionPredicate((_, api) =>
    {
        var path = api.RelativePath ?? "";
        return !path.Contains("$metadata") && path != "odata";
    });

    // Cấu hình nút Authorize JWT Bearer trên Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT token theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
