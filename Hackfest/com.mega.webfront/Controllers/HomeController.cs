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
        public async Task<IActionResult> Index(string sessionType, string username)
        {
            ViewData["Hostname"] = Environment.MachineName;

            var queueClient = QueueClient.Create("RequestQueue");
            var message = new QueueMessage(sessionType, username);

            var response = await queueClient.PushAsync(message);
            ViewData["FormData"] = response.Item2.MessageId;
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
