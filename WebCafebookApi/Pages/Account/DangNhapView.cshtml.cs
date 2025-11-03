using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CafebookModel.Model.ModelApi; // Thêm
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json; // Thêm

namespace WebCafebookApi.Pages.Account
{
    public class DangNhapViewModel : PageModel
    {
        // 1. XÓA DbContext, THAY BẰNG HttpClientFactory
        private readonly IHttpClientFactory _httpClientFactory;

        public DangNhapViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập, Email hoặc SĐT")]
            [Display(Name = "Tên đăng nhập, Email hoặc SĐT")]
            public string LoginIdentifier { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Ghi nhớ đăng nhập")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }
            returnUrl ??= Url.Content("~/");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 2. TẠO YÊU CẦU GỌI API
            var httpClient = _httpClientFactory.CreateClient();
            var apiRequest = new LoginRequestModel
            {
                TenDangNhap = Input.LoginIdentifier,
                MatKhau = Input.Password
            };

            // Lấy URL của API (giả sử là localhost:5166)
            var response = await httpClient.PostAsJsonAsync("http://localhost:5166/api/web/taikhoankhach/login", apiRequest);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<WebLoginResponseModel>();

                if (apiResponse != null && apiResponse.Success && apiResponse.KhachHangData != null)
                {
                    // 3. ĐĂNG NHẬP THÀNH CÔNG -> TẠO COOKIE
                    var user = apiResponse.KhachHangData;
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.IdKhachHang.ToString()),
                        new Claim(ClaimTypes.Name, user.TenDangNhap ?? user.Email ?? ""),
                        new Claim(ClaimTypes.GivenName, user.HoTen),
                        new Claim(ClaimTypes.Email, user.Email ?? ""),
                        new Claim(ClaimTypes.MobilePhone, user.SoDienThoai ?? ""),
                        new Claim(ClaimTypes.Role, "KhachHang") // Gán Role
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties { IsPersistent = Input.RememberMe };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // 4. LƯU AVATAR (Mặc định, vì KhachHang không có)
                    HttpContext.Session.SetString("AvatarBase64", "");

                    return LocalRedirect(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, apiResponse?.Message ?? "Lỗi không xác định từ API.");
                    return Page();
                }
            }

            ModelState.AddModelError(string.Empty, "Không thể kết nối đến máy chủ đăng nhập.");
            return Page();
        }
    }
}