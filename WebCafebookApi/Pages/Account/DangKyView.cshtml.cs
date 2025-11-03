using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CafebookModel.Model.ModelApi; // Thêm
using CafebookModel.Model.ModelWeb; // Thêm
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json; // Thêm

// XÓA CÁC USING LIÊN QUAN ĐẾN DbContext VÀ ENTITIES
// using Microsoft.EntityFrameworkCore;
// using CafebookModel.Model.Entities;

namespace WebCafebookApi.Pages.Account
{
    public class DangKyViewModel : PageModel
    {
        // SỬA LỖI CS0246: Xóa DbContext, dùng HttpClientFactory
        private readonly IHttpClientFactory _httpClientFactory;
        public DangKyViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // SỬA LỖI CS8618: Khởi tạo Input
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // SỬA LỖI CS8618: Dùng string? và khởi tạo
        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            // SỬA LỖI CS8618: Khởi tạo string
            [Required(ErrorMessage = "Vui lòng nhập họ tên")]
            [Display(Name = "Họ và Tên")]
            public string HoTen { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập SĐT")]
            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            [Display(Name = "Số điện thoại")]
            public string SoDienThoai { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập Email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Tên đăng nhập")]
            public string? TenDangNhap { get; set; } // Nullable là OK

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
            [StringLength(100, ErrorMessage = "{0} phải dài từ {2} đến {1} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu")]
            [Compare("Password", ErrorMessage = "Mật khẩu và mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null) // Sửa lỗi CS8625
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null) // Sửa lỗi CS8625
        {
            returnUrl ??= Url.Content("~/");
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // GỌI API ĐĂNG KÝ
            var httpClient = _httpClientFactory.CreateClient();
            var apiRequest = new DangKyRequestModel
            {
                HoTen = Input.HoTen,
                Email = Input.Email,
                SoDienThoai = Input.SoDienThoai,
                TenDangNhap = Input.TenDangNhap,
                Password = Input.Password
            };

            var response = await httpClient.PostAsJsonAsync("http://localhost:5166/api/web/taikhoankhach/register", apiRequest);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<WebLoginResponseModel>();

                if (apiResponse != null && apiResponse.Success && apiResponse.KhachHangData != null)
                {
                    // TỰ ĐỘNG ĐĂNG NHẬP SAU KHI ĐĂNG KÝ
                    var user = apiResponse.KhachHangData;
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.IdKhachHang.ToString()),
                        new Claim(ClaimTypes.Name, user.TenDangNhap ?? user.Email ?? ""),
                        new Claim(ClaimTypes.GivenName, user.HoTen),
                        new Claim(ClaimTypes.Email, user.Email ?? ""),
                        new Claim(ClaimTypes.MobilePhone, user.SoDienThoai ?? ""),
                        new Claim(ClaimTypes.Role, "KhachHang")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    return LocalRedirect(returnUrl);
                }
                else
                {
                    // Hiển thị lỗi từ API (ví dụ: "Email đã tồn tại")
                    ModelState.AddModelError(string.Empty, apiResponse?.Message ?? "Lỗi đăng ký.");
                    return Page();
                }
            }

            ModelState.AddModelError(string.Empty, "Không thể kết nối máy chủ đăng ký.");
            return Page();
        }
    }
}