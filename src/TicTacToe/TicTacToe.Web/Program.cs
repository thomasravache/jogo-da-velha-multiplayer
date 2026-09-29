using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.PlayerIdentity;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ShellState>();
builder.Services.AddScoped<IPlayerStorage, BrowserPlayerStorage>();
builder.Services.AddScoped<PlayerIdentityService>();
builder.Services.AddOutputCache();

// Módulo Matchmaking
builder.Services.AddSingleton<MatchmakingService>();
builder.Services.AddSingleton<ConcurrentDictionary<Guid, GameSession>>();

// Módulo Gameplay — DbContext via Aspire (injeta connection string automaticamente)
builder.AddSqlServerDbContext<GameplayDbContext>("TicTacToeDb");
builder.Services.AddScoped<GameResultService>();

var app = builder.Build();

// Aplicar migrations automaticamente no startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameplayDbContext>();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseOutputCache();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
