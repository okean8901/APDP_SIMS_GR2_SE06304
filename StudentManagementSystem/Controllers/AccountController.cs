﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Models.DTOs;
using StudentManagementSystem.Models.Entities;
using StudentManagementSystem.Repositories;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StudentManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Student> _studentRepository;

        public AccountController(IRepository<User> userRepository, IRepository<Student> studentRepository)
        {
            _userRepository = userRepository;
            _studentRepository = studentRepository;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(UserDTO model)
        {
            // Loại bỏ lỗi validation cho Role để cho phép nó rỗng
            if (ModelState.ContainsKey("Role"))
            {
                ModelState["Role"].Errors.Clear();
                ModelState["Role"].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userRepository.GetByUsernameAsync(model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Username đã được đăng ký, vui lòng dùng Username khác.");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                Password = model.Password, // Nên dùng BCrypt để mã hóa
                Email = model.Email,
                FullName = model.FullName,
                UserRoles = new List<UserRole>()
            };

            await _userRepository.AddAsync(user);

            // Gán vai trò: Nếu không chọn (rỗng hoặc null), mặc định là "Admin"
            string role = string.IsNullOrEmpty(model.Role) ? "Admin" : model.Role;
            await _userRepository.AssignRoleAsync(user.UserId, role);

            // Nếu vai trò là "Student", thêm thông tin vào bảng Student
            if (role == "Student")
            {
                var student = new Student
                {
                    UserId = user.UserId,
                    FullName = model.FullName,
                    DateOfBirth = model.DateOfBirth,
                    Address = model.Address,
                    Major = model.Major,
                    BatchYear = model.BatchYear,
                    Status = "Active",
                    Class = "Unknown"
                };
                await _studentRepository.AddAsync(student);
            }

            model.IsRegisteredSuccessfully = true;
            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                return View();
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null || user.Password != password)
            {
                ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
            };

            if (user.UserRoles != null)
            {
                foreach (var userRole in user.UserRoles)
                {
                    if (userRole.Role != null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, userRole.Role.RoleName));
                    }
                }
            }

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync("CookieAuth", claimsPrincipal);

            TempData["Username"] = user.Username;
            TempData["SuccessMessage"] = "Login Successful!";

            if (user.UserRoles.Any(ur => ur.Role.RoleName == "Teacher"))
            {
                return RedirectToAction("Index", "Teacher");
            }
            else if (user.UserRoles.Any(ur => ur.Role.RoleName == "Student"))
            {
                return RedirectToAction("Index", "Student");
            }
            else if (user.UserRoles.Any(ur => ur.Role.RoleName == "Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }
    }
}