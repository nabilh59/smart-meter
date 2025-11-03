using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartMeterServer.Hubs;     // FirstHub
using SmartMeterServer.Models;   

namespace SmartMeterServer.Controllers
{
    [ApiController]
    [Route("api/grid")]
    public class GridController : ControllerBase
    {
        private readonly IHubContext<FirstHub> _hub; 

        public GridController(IHubContext<FirstHub> hub) => _hub = hub;

        [HttpGet("status")]
        public IActionResult Status() => Ok(new { status = GridState.Current });

        [HttpPost("down")]
        public async Task<IActionResult> Down()
        {
            GridState.Current = "DOWN";
            var msg = new GridStatusMessage(
                "grid.status", "1.0", "DOWN", "PAUSE_READINGS",
                "Temporary grid interruption",
                "We can’t receive readings right now due to a grid issue. No action is needed.",
                DateTime.UtcNow
            );
            await _hub.Clients.All.SendAsync("gridStatus", msg);
            return Ok(new { ok = true });
        }

        [HttpPost("up")]
        public async Task<IActionResult> Up()
        {
            GridState.Current = "UP";
            var msg = new GridStatusMessage(
                "grid.status", "1.0", "UP", "RESUME_READINGS",
                "Grid back to normal",
                "Readings will resume automatically.",
                DateTime.UtcNow
            );
            await _hub.Clients.All.SendAsync("gridStatus", msg);
            return Ok(new { ok = true });
        }
    }
}
