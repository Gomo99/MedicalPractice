using MedicalPractice.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MedicalPractice.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("DashBoard", "Admin");
                else if (User.IsInRole("Doctor"))
                    return RedirectToAction("DashBoard", "Doctors");
                else if (User.IsInRole("Assistant"))
                    return RedirectToAction("DashBoard", "Assistance");
                else if (User.IsInRole("Receptionist"))
                    return RedirectToAction("DashBoard", "Receptionist");
<<<<<<< HEAD

=======
>>>>>>> 1be8920d85f2525285b3857d91b8d60e48c4ea49
            }
            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel
            { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}