// Tập tin: WebCafebookApi/Pages/employee/DangNhapEmployee.cshtml.cs
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CafebookModel.Model.ModelApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using CafebookModel.Model.Data;

namespace WebCafebookApi.Pages.employee
{
    public class DangNhapEmployeeModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public DangNhapEmployeeModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập tài khoản")]
            public string TenDangNhap { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
            [DataType(DataType.Password)]
            public string MatKhau { get; set; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient();
            var apiRequest = new LoginRequestModel
            {
                TenDangNhap = Input.TenDangNhap,
                MatKhau = Input.MatKhau
            };
            var response = await httpClient.PostAsJsonAsync("http://localhost:5166/api/web/taikhoannv/login", apiRequest);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<WebLoginResponseModel>();
                if (apiResponse != null && apiResponse.Success && apiResponse.NhanVienData != null)
                {
                    var user = apiResponse.NhanVienData;
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.IdNhanVien.ToString()),
                        new Claim(ClaimTypes.Name, user.HoTen ?? ""),
                        new Claim(ClaimTypes.GivenName, user.HoTen ?? ""),
                        new Claim(ClaimTypes.Role, user.TenVaiTro ?? "NhanVien")
                    };
                    foreach (var quyen in user.DanhSachQuyen)
                    {
                        claims.Add(new Claim("Permission", quyen));
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // SỬA: Lưu URL thay vì Base64 và đổi tên Key
                    HttpContext.Session.SetString("AvatarUrl", user.AnhDaiDien ?? "");

                    // Chuyển hướng đến trang Dashboard
                    return LocalRedirect(Url.Content("~/Employee/Dashboard"));
                }
                else
                {
                    ErrorMessage = apiResponse?.Message ?? "Lỗi không xác định từ API.";
                    return Page();
                }
            }

            ErrorMessage = "Không thể kết nối đến máy chủ đăng nhập.";
            return Page();
        }
    }
}