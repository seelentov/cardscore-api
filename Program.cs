using cardscore_api.Data;
using cardscore_api.Middlewares;
using cardscore_api.Services;
using cardscore_api.Components;
using Microsoft.EntityFrameworkCore;
using Blazored.LocalStorage;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddControllers().AddNewtonsoftJson(options =>
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DataContext>(options => options.UseSqlite($"Data Source=db.db"));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = $"localhost:6379";
});

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddTransient<UserService>();
builder.Services.AddTransient<RoleService>();
builder.Services.AddTransient<BCryptService>();
builder.Services.AddTransient<JwtService>();

builder.Services.AddTransient<Soccer365ParserService>();

builder.Services.AddTransient<LeagueParseListService>();
builder.Services.AddTransient<LeaguesService>();
builder.Services.AddTransient<UrlService>();
builder.Services.AddTransient<InfosService>();
builder.Services.AddTransient<ReglamentsService>();
builder.Services.AddTransient<UserNotificationOptionService>();
builder.Services.AddTransient<ErrorsService>();
builder.Services.AddTransient<RedisService>();
builder.Services.AddTransient<NotificationsService>();
builder.Services.AddTransient<ExpoNotificationsService>();

builder.Services.AddHostedService<BackupService>();
builder.Services.AddHostedService<NotificationWorkerService>();

var app = builder.Build();

app.UseMiddleware<JwtMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseMiddleware<BasicAuthMiddleware>();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
