using Microsoft.AspNetCore.Mvc;
using Sino.Extensions.EventBus.Attributes;
using Sino.Extensions.EventBus.Common;
using Sino.Extensions.EventBus.Consumer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestWebApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return Ok();
        }

        public IActionResult About()
        {
            return Ok();
        }

        public IActionResult Contact()
        {
            return Ok();
        }

        public IActionResult Error()
        {
            return Ok();
        }
    }
}
