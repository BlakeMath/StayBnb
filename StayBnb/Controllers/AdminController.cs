using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StayBnb.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]//admin or superadmin can access this controller
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
