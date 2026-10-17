using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
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

// TODO (Dev 1): cấu hình JWT Authentication ở đây

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
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.Run();
