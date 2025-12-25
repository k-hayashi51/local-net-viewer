using LocalNetViewer.Constants;
using LocalNetViewer.Models;
using LocalNetViewer.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Sockets;

namespace LocalNetViewer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        [HttpGet("position")]
        public ActionResult<string> GetPosition()
        {
            return Ok(SettingsManager.Position);
        }

        [HttpPatch("position")]
        public IActionResult PutPosition([FromBody] string position)
        {
            SettingsManager.Position = position;
            return Ok();
        }

        [HttpGet("imagePageMode")]
        public ActionResult<string> GetImagePageMode()
        {
            return Ok(SettingsManager.ImagePageMode);
        }

        [HttpPatch("imagePageMode")]
        public IActionResult PutImagePageMode([FromBody] ImagePageMode imagePageMode)
        {
            SettingsManager.ImagePageMode = imagePageMode;
            return Ok();
        }
    }
}
