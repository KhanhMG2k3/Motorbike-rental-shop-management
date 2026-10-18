using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.WebEncoders;
using MotoRent.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Giữ nguyên tiếng Việt trong HTML (mặc định Razor encode thành &#xE0;...)
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// ===== Gọi Web API =====
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
    ?? throw new InvalidOperationException("Thiếu cấu hình ApiSettings:BaseUrl trong appsettings.json");

void ConfigureApiClient(HttpClient client)
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenHandler>();

builder.Services.AddHttpClient<MotorbikeApiClient>(ConfigureApiClient);
builder.Services.AddHttpClient<CatalogAdminApiClient>(ConfigureApiClient)
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<MaintenanceApiClient>(ConfigureApiClient)
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<BranchApiClient>(ConfigureApiClient)
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<UserAdminApiClient>(ConfigureApiClient)
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<AuthApiClient>(ConfigureApiClient)
    .AddHttpMessageHandler<BearerTokenHandler>();

// ===== Cookie Authentication =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "MotoRent.Auth";
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Số dạng 180000.50 (dấu chấm thập phân) khi bind form, không phụ thuộc ngôn ngữ của Windows.
// Hiển thị tiền tệ kiểu Việt Nam dùng DisplayHelper.Money (vi-VN).
var invariantCulture = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(invariantCulture),
    SupportedCultures = new[] { invariantCulture },
    SupportedUICultures = new[] { invariantCulture }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Trang quản trị: /Admin/{controller}/{action}
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Motorbikes}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
