using SmartMeter.Hubs;
using System.Globalization;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IMeterStore, MeterStore>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

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

// debug: return all meters as JSON via the injected store (meters with timestamp->reading)
app.MapGet("/debug/readings", (IMeterStore store) =>
{
    var culture = CultureInfo.GetCultureInfo("en-GB");
    var initial = store.InitialBill;
    const double pricePerKwh = 0.15; // match FirstHub constant

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
                totalCostFormatted = totalCost.ToString("C2", culture),
                totalBill = total,
                totalBillFormatted = total.ToString("C2", culture),
                initialBillFormatted = initial.ToString("C2", culture)
            };
        });
    return Results.Json(snapshot);
});

app.Run();
