using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkGroupPortal.Models;

namespace WorkGroupPortal.Controllers
{
    public class HomeController : Controller
    {
        //private readonly ILogger<HomeController> _logger;
        private readonly LabDBContext _context;

        public HomeController(LabDBContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // getting UserId from Session
            var userIdString = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdString))
            {
                int userId = int.Parse(userIdString);

                // Finding user's Id
                var user = _context.Users.FirstOrDefault(c => c.Id == userId);

                if (user != null)
                {
                    // Passing user object
                    return View(user);
                }
            }
            // If not found back to login
            return View(null);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // If fails, return the same view with validation messages
                return View(model);
            }

            // Check if the user exists in the database
            var user = _context.Users
                .FirstOrDefault(u => u.Username == model.Username && u.Password == model.Password);

            if (user == null)
            {
                // Error
                ModelState.AddModelError("Username", "Invalid username or password.");
                return View(model);
            }

            // User's ID in session
            HttpContext.Session.SetString("UserId", user.Id.ToString());

            // Success
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            var user = new User();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignUp(User user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            // Check for duplicate Email
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email is already in use.");
                return View(user);
            }

            // Check for duplicate Username (optional, if Username is unique)
            if (_context.Users.Any(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "Username is already taken.");
                return View(user);
            }

            // Set additional fields
            user.CreatedAt = DateTime.UtcNow;

            // Hash the password (very important!)
            //user.Password = PasswordHelper.HashPassword(user.Password);
            //this needs a new class and method

            // Add and save user in the database
            _context.Users.Add(user);
            _context.SaveChanges();

            // Redirect to Login or another page
            return RedirectToAction("Login");
        }



        public IActionResult Logout()
        {
            // Clear sessio
            HttpContext.Session.Clear();

            // Back to Index
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
