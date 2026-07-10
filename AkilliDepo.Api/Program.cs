using System.Text;
using AkilliDepo.Api.Data;
using AkilliDepo.Api.Managers;
using AkilliDepo.Api.Middleware;
using AkilliDepo.Api.Repositories;
using AkilliDepo.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Yanıtlar PascalCase döner (frontend camelCase'e çevirir)
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.DictionaryKeyPolicy = null;
});
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<IBrandManager, BrandManager>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductManager, ProductManager>();
builder.Services.AddScoped<IGoodsReceiptRepository, GoodsReceiptRepository>();
builder.Services.AddScoped<IGoodsReceiptManager, GoodsReceiptManager>();
builder.Services.AddScoped<IBoxRepository, BoxRepository>();
builder.Services.AddScoped<IBoxManager, BoxManager>();
builder.Services.AddScoped<IStockAdjustmentRepository, StockAdjustmentRepository>();
builder.Services.AddScoped<IReportDataRepository, ReportDataRepository>();
builder.Services.AddScoped<IWeeklyReportRepository, WeeklyReportRepository>();
builder.Services.AddScoped<IWeeklyReportManager, WeeklyReportManager>();
builder.Services.AddHostedService<WeeklyReportBackgroundService>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<ILocationManager, LocationManager>();
builder.Services.AddScoped<IStoreOrderRepository, StoreOrderRepository>();
builder.Services.AddScoped<IStoreOrderManager, StoreOrderManager>();
builder.Services.AddScoped<IPdfOrderParser, PdfOrderParser>();
builder.Services.AddScoped<IStoreRepository, StoreRepository>();
builder.Services.AddScoped<IStoreManager, StoreManager>();
builder.Services.AddScoped<ICompanySettingsRepository, CompanySettingsRepository>();
builder.Services.AddScoped<ICompanySettingsManager, CompanySettingsManager>();
builder.Services.AddScoped<IDispatchRepository, DispatchRepository>();
builder.Services.AddScoped<IDispatchPalletRepository, DispatchPalletRepository>();
builder.Services.AddScoped<IDispatchManager, DispatchManager>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key yapılandırılmamış. 'dotnet user-secrets set Jwt:Key <deger>' ile ayarlayın.");

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
