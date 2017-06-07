using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceBus.Messaging;
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
        public async Task<IActionResult> Index(string vote)
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
