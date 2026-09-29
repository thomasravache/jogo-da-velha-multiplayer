using TicTacToe.Web.Components;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Modules.Gameplay;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
    await db.Database.EnsureCreatedAsync();
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
