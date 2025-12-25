using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LocalNetViewer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NetworkController : ControllerBase
    {
        [HttpGet("url")]
        public IActionResult GetServerUrl()
        {
            var ip = GetLocalIPAddress();
            return Ok($"http://{ip}:5197");
        }

        private static string GetLocalIPAddress()
        {
            var result = string.Empty;
            foreach (var ni in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ni.AddressFamily == AddressFamily.InterNetwork) // IPv4
                {
                    result = ni.ToString();
                    break;
                }
            }
            return result;
        }
    }
}
