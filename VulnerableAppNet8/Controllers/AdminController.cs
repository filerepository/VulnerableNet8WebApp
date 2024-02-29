using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VulnerableAppNet8.Models;


namespace VulnerableAppNet8.Controllers
{
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;

        public AdminController(ILogger<AdminController> logger)
        {
            _logger = logger;
        }
        public IActionResult Index()
        {
            var headers = Request.Headers;
            var headersList = new List<string>();
            foreach (var header in headers)
            {
                headersList.Add($"{header.Key}: {header.Value}");
            }

            // vulnerable code that does not care about multiple values for the header
            // in a more real world example, it is up to the load balancer or gateway to decide what will happen if
            // the client has already added a header with the same name.
            // this code should make the comparison against the last value (comma separated) for the header, because that
            // is added by the load balancer (or gateway or some other network component)
            var clientIp = string.Empty;
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                clientIp = Request.Headers["X-Forwarded-For"];
            }
            else
            {
                clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            }

            ViewData["ClientIp"] = clientIp;

            if (clientIp == "127.0.0.1")
            {
                return View();
            }
            else
            {
                return View("Forbidden", headersList);
            }

        }
    }
}
