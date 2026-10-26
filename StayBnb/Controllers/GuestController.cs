using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StayBnb.Controllers
{
    [Authorize(Roles = "Guest")]
    public class GuestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
