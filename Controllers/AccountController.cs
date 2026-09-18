using CineBook.Models.DTOs;
using Microsoft.AspNetCore.Authentication;          // ← add: gives us SignInAsync / SignOutAsync
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;

namespace CineBook.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        private async Task SignInUserAsync(AuthResponseData userData)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userData.UserId.ToString()),
        new Claim(ClaimTypes.Name, userData.Name),
        new Claim(ClaimTypes.Email, userData.Email),
        new Claim(ClaimTypes.Role, userData.Role)
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequestDto model, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            var client = _httpClientFactory.CreateClient("BackendApi");

            var response = await client.PostAsJsonAsync("/api/auth/login", model);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<AuthResponseData>>();

            if (result == null || !result.Success)
            {
                ViewBag.Error = result?.Message ?? "Login failed.";
                return View();
            }

            HttpContext.Session.SetString("JwtToken", result.Data.Token);
            HttpContext.Session.SetString("UserName", result.Data.Name);
            HttpContext.Session.SetString("UserEmail", result.Data.Email);
            await SignInUserAsync(result.Data);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterRequestDto model, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            var client = _httpClientFactory.CreateClient("BackendApi");

            var response = await client.PostAsJsonAsync("/api/auth/register", model);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Registration failed. Please check your details.";
                return View();
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<AuthResponseData>>();

            if (result == null || !result.Success)
            {
                ViewBag.Error = result?.Message ?? "Registration failed.";
                return View();
            }

            // Register response mein token nahi aata, isliye turant Login call karte hain
            var loginModel = new LoginRequestDto
            {
                Email = model.Email,
                Password = model.Password
            };

            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginModel);

            if (loginResponse.IsSuccessStatusCode)
            {
                var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponseWrapper<AuthResponseData>>();

                if (loginResult != null && loginResult.Success)
                {
                    HttpContext.Session.SetString("JwtToken", loginResult.Data.Token);
                    HttpContext.Session.SetString("UserName", loginResult.Data.Name);
                    HttpContext.Session.SetString("UserEmail", loginResult.Data.Email);
                    await SignInUserAsync(loginResult.Data);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }
            }

            // Agar kisi wajah se auto-login fail ho jaaye, to Login page pe bhej do
            return RedirectToAction("Login", new { returnUrl });
        }

        public async Task<IActionResult> Logout()   // ← add 'async' here
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}