using SmartMeter.Hubs;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<SmartMeter.Hubs.IInMemoryDatabase, SmartMeter.Hubs.InMemoryDatabase>();
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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

// apply CORS before endpoints that need it
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapRazorPages();
app.MapHub<FirstHub>("/hubs/connect");

// debug: return all readings as JSON (clientId -> info)
app.MapGet("/debug/readings", (SmartMeter.Hubs.IInMemoryDatabase db) =>
{
    var initial = db.InitialBill;
    var snapshot = db.Readings.ToDictionary(
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
