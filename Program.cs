using cardscore_api.Data;
using cardscore_api.Middlewares;
using cardscore_api.Services;
using cardscore_api.Components;
using Microsoft.EntityFrameworkCore;
using Blazored.LocalStorage;
using cardscore_api.Services.ParserServices;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddControllers().AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        }
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DataContext>(options => options.UseSqlite($"Data Source=db.db"));

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddTransient<RedisService>();
builder.Services.AddTransient<UserService>();
builder.Services.AddTransient<RoleService>();
builder.Services.AddTransient<BCryptService>();
builder.Services.AddTransient<JwtService>();

builder.Services.AddTransient<Soccer365ParserService>();
builder.Services.AddSingleton<SoccerwayParserService>();

builder.Services.AddTransient<ParserService>();
builder.Services.AddTransient<LeagueParseListService>();
builder.Services.AddTransient<FetchService>();
builder.Services.AddTransient<LeaguesService>();
builder.Services.AddTransient<UrlService>();
builder.Services.AddTransient<InfosService>();
builder.Services.AddTransient<ReglamentsService>();
builder.Services.AddTransient<UserNotificationOptionService>();
builder.Services.AddTransient<ErrorsService>();
builder.Services.AddTransient<NotificationsService>();
builder.Services.AddTransient<ExpoNotificationsService>();
builder.Services.AddTransient<BaseOptionsService>();
builder.Services.AddTransient<FormatService>();
builder.Services.AddSingleton<SeleniumService>();
builder.Services.AddSingleton<AsyncService>();


builder.Services.AddHostedService<BackupService>(); 
builder.Services.AddHostedService<NotificationWorkerService>();
/*builder.Services.AddHostedService<GamesWorkerService>();*/

builder.Host.ConfigureLogging(opt =>
{   
    opt.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    opt.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
});

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

app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
