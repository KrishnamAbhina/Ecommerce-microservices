using System.Text;
using CartAPI.DBcontext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

foreach (var setting in new[] { "Jwt:Key", "Jwt:Audience" })
{
    if (string.IsNullOrWhiteSpace(builder.Configuration[setting]))
    {
        throw new InvalidOperationException($"{setting} must be configured.");
    }
}

if (builder.Configuration.GetSection("Jwt:Issuers").Get<string[]>() is not { Length: > 0 })
{
    throw new InvalidOperationException("Jwt:Issuers must list at least one issuer.");
}

if (Encoding.UTF8.GetByteCount(builder.Configuration["Jwt:Key"]!) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");
}

builder.Services.AddDbContext<CartDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient("ProductsAPI", client =>
    client.BaseAddress = new Uri(builder.Configuration["ProductsAPI:BaseUrl"]!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuers = builder.Configuration.GetSection("Jwt:Issuers").Get<string[]>(),
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

ApplyMigrations(app);

app.Run();

void ApplyMigrations(IHost app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();
    if (dbContext.Database.GetPendingMigrations().Any())
    {
        dbContext.Database.Migrate();
    }
}
