var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// サービス登録
// ----------------------------
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ----------------------------
// HTTP / LAN 向けバインド
// ----------------------------
builder.WebHost.UseUrls("http://0.0.0.0:5197");

// ----------------------------
// アプリ生成
// ----------------------------
var app = builder.Build();

// ----------------------------
// WWWROOT
// ----------------------------
app.UseDefaultFiles();
app.UseStaticFiles();

// ----------------------------
// 開発環境のみ OpenAPI
// ----------------------------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ----------------------------
// ルーティング
// ----------------------------
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

// ----------------------------
// 起動後にブラウザを開く（HTTP）
// ----------------------------
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(() =>
{
    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
    {
        FileName = "http://localhost:5197",
        UseShellExecute = true
    });
});

app.Run();