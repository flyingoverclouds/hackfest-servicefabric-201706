using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using com.mega.queuecontract;

namespace com.mega.webfront.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Hostname"] = Environment.MachineName;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string language)
        {
            ViewData["Hostname"] = Environment.MachineName;
            ViewData["FormData"] = language;

            var queueClient = QueueClient.Create("QueueService");

            var message = new QueueMessage()
            {
                Language = language,
            };
            await queueClient.PushAsync(message);
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
