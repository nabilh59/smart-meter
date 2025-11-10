using SmartMeter.Hubs;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// configure a named CORS policy used later
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapRazorPages();
app.MapHub<FirstHub>("/hubs/connect");

// debug: return all readings as JSON (clientId -> info) using FirstHub's dictionary
app.MapGet("/debug/readings", () =>
{
    var initial = FirstHub.InitialBill;
    var snapshot = FirstHub.Readings.ToDictionary(
        kv => kv.Key,
        kv =>
        {
            var readings = kv.Value.ToArray();
            var sumReadings = readings.Sum();
            var total = initial + sumReadings;
            return new
            {
                initialBill = initial,
                readings = readings,
                sumReadings = sumReadings,
                totalBill = total
            };
        });
    return Results.Json(snapshot);
});

app.Run();
