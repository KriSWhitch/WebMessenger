using Microsoft.EntityFrameworkCore;
using WebMessenger.DAL.Data;
using WebMessenger.DAL.Interfaces;
using WebMessenger.DAL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Api.Services;
using WebMessenger.Api.Hubs;
using WebMessenger.Api.Hubs.Events.Interfaces;
using WebMessenger.Api.Hubs.Events;
using WebMessenger.Api.Infrastructure.Interfaces;
using WebMessenger.Api.Infrastructure;
using WebMessenger.Api.Services.FileStorage;
using Microsoft.Extensions.Hosting;
using Serilog;

// Bootstrap logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting WebMessenger API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
                     .ReadFrom.Services(services)
                     .Enrich.FromLogContext());

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        };
    });

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new MySqlServerVersion(new Version(8, 0, 42)),
            mysqlOptions => { mysqlOptions.EnableRetryOnFailure(); }
        ));

    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IContactsService, ContactsService>();
    builder.Services.AddScoped<IAvatarService, AvatarService>();
    builder.Services.AddScoped<IChatService, ChatService>();
    builder.Services.AddScoped<IChatEvents, ChatEvents>();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();
    builder.Services.AddScoped<IFileStorage, DropboxFileStorage>();

    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = true;
    });

    builder.Services.AddHttpContextAccessor();

    const string FrontCors = "Front";
    builder.Services.AddCors(opts =>
    {
        opts.AddPolicy(name: FrontCors, policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:3000",
                    "https://localhost:3000"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
                )
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var path = context.HttpContext.Request.Path;
                    if (path.StartsWithSegments("/hubs/chat"))
                    {
                        if (context.Request.Query.TryGetValue("access_token", out var tokenFromQuery))
                        {
                            context.Token = tokenFromQuery;
                            return Task.CompletedTask;
                        }
                        if (context.Request.Cookies.TryGetValue("auth-token", out var tokenFromCookie))
                        {
                            context.Token = tokenFromCookie;
                            return Task.CompletedTask;
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });

    var app = builder.Build();

    const int migrationMaxAttempts = 10;
    for (var attempt = 1; attempt <= migrationMaxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
            Log.Information("Database migration completed");
            break;
        }
        catch (Exception ex) when (attempt < migrationMaxAttempts)
        {
            Log.Warning(ex, "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying...", attempt, migrationMaxAttempts);
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseExceptionHandler();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseCors(FrontCors);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHub<ChatHub>("/hubs/chat").RequireCors(FrontCors);

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
