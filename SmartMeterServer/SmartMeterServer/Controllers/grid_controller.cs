using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartMeter.Hubs;
using SmartMeterServer.Models;

namespace SmartMeter.Controllers
{
    [ApiController]
    [Route("api/grid")]
    public class GridController : ControllerBase
    {
        private readonly IHubContext<AlertsHub> _hub;
        private static string _current = "UP"; // for status checks

        public GridController(IHubContext<AlertsHub> hub) => _hub = hub;

        [HttpGet("status")]
        public IActionResult Status() => Ok(new { status = _current });

        [HttpPost("down")]
        public async Task<IActionResult> Down()
        {
            _current = "DOWN";
            var msg = new GridStatusMessage(
                Type: "grid.status",
                SchemaVersion: "1.0",
                Status: "DOWN",
                ClientAction: "PAUSE_READINGS",
                Title: "Temporary grid interruption",
                Message: "We can’t receive readings right now due to a grid issue. No action is needed.",
                RaisedAtUtc: DateTime.UtcNow
            );
            await _hub.Clients.All.SendAsync("gridStatus", msg);
            return Ok(new { ok = true });
        }

        [HttpPost("up")]
        public async Task<IActionResult> Up()
        {
            _current = "UP";
            var msg = new GridStatusMessage(
                Type: "grid.status",
                SchemaVersion: "1.0",
                Status: "UP",
                ClientAction: "RESUME_READINGS",
                Title: "Grid back to normal",
                Message: "Readings will resume automatically.",
                RaisedAtUtc: DateTime.UtcNow
            );
            await _hub.Clients.All.SendAsync("gridStatus", msg);
            return Ok(new { ok = true });
        }
    }
}
