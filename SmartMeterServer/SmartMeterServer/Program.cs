using SmartMeterServer.Hubs;
using SmartMeterServer.Models;
using System.Globalization;
using System.Linq;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddSingleton<IMeterStore, MeterStore>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
        policy.WithOrigins("https://localhost:5001") // Allow requests from this url
              .AllowAnyHeader()
              .AllowAnyMethod() // Allow all http methods
              .SetIsOriginAllowed(_ => true)); // dev-friendly; tighten later
});

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = (int)HttpStatusCode.PermanentRedirect;
    options.HttpsPort = 443;
});

builder.Services.AddControllers();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error"); 
}

app.UseHttpsRedirection();
app.UseHsts();

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowSpecificOrigin");
app.UseAuthorization();
app.MapHub<FirstHub>("/hubs/connect");

// debug: return all meters as JSON via the injected store (meters with timestamp->reading)
app.MapGet("/debug/readings", (IMeterStore store) =>
{
    var culture = CultureInfo.GetCultureInfo("en-GB");
    var initial = store.initialBill;
    double pricePerKwh = store.PricePerKwh;


	var snapshot = store.GetAll().ToDictionary(
        kv => kv.Key,
        kv =>
        {
            var meter = kv.Value;

            // produce an array of timestamped readings with formatted date/time
            var readings = meter.Snapshot()
                         .OrderBy(kv2 => kv2.Key)
                         .Select(kv2 =>
                         {
                             var dt = DateTimeOffset.FromUnixTimeMilliseconds(kv2.Key).ToLocalTime();
                             return new
                             {
                                 date = dt.ToString("dd-MM-yyyy"),         // dd-mm-yyyy
                                 time = dt.ToString("HH:mm"),              // HH:mm
                                 value = kv2.Value
                             };
                         })
                         .ToArray();

            var sumReadings = meter.SumReadings();
            var totalCost = sumReadings * pricePerKwh;
            var total = initial + totalCost;

            return new
            {
                connectionId = meter.ID,
                readingCount = meter.ReadingCount,
                readings = readings, // array of { timestamp, date, time, value }
                sumReadings = sumReadings,
                totalCost = totalCost,
                totalCostFormatted = totalCost.ToString(culture),
                totalBill = total,
                totalBillFormatted = total.ToString(culture),
                initialBillFormatted = initial.ToString(culture)
            };
        });
    return Results.Json(snapshot);
});

app.MapControllers();

app.Run();

