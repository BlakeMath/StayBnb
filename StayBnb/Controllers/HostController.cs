using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StayBnb.Controllers
{
    [Authorize(Roles = "Host")]
    public class HostController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
