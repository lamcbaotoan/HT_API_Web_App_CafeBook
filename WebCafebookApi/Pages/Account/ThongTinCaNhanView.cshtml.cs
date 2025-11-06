using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebCafebookApi.Pages.Account
{
    [Authorize(Roles = "KhachHang")]
    public class ThongTinCaNhanViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        [BindProperty]
        public ProfileUpdateModel Input { get; set; } = new();

        [BindProperty]
        public IFormFile? AvatarFile { get; set; }

        public string TenDangNhap { get; set; } = string.Empty;
        public string AvatarHienTaiUrl { get; set; } = string.Empty;

        public bool IsEditMode { get; set; } = false;

        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }

        public ThongTinCaNhanViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);
            return userId;
        }

        public async Task<IActionResult> OnGetAsync(string? handler)
        {
            // SỬA LỖI CS0103: Xóa dòng "Layout = ..." ở đây.
            var userId = GetCurrentUserId();
            if (userId == 0) return Challenge();

            if (handler == "Edit")
            {
                IsEditMode = true;
            }

            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                var profile = await httpClient.GetFromJsonAsync<KhachHangProfileDto>($"http://localhost:5166/api/web/profile/{userId}");
                if (profile != null)
                {
                    Input.HoTen = profile.HoTen;
                    Input.SoDienThoai = profile.SoDienThoai;
                    Input.Email = profile.Email;
                    Input.DiaChi = profile.DiaChi;
                    TenDangNhap = profile.TenDangNhap ?? "(Chưa có)";
                    AvatarHienTaiUrl = profile.AnhDaiDienUrl ?? "/images/default-avatar.png";
                }
                return Page();
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi tải thông tin: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Challenge();
            if (!ModelState.IsValid)
            {
                return await OnGetAsync("Edit");
            }

            var httpClient = _httpClientFactory.CreateClient();
            using var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(Input.HoTen), nameof(Input.HoTen));
            formData.Add(new StringContent(Input.SoDienThoai ?? ""), nameof(Input.SoDienThoai));
            formData.Add(new StringContent(Input.Email ?? ""), nameof(Input.Email));
            formData.Add(new StringContent(Input.DiaChi ?? ""), nameof(Input.DiaChi));

            if (AvatarFile != null)
            {
                formData.Add(new StreamContent(AvatarFile.OpenReadStream()), "avatarFile", AvatarFile.FileName);
            }

            try
            {
                var response = await httpClient.PutAsync($"http://localhost:5166/api/web/profile/update-info/{userId}", formData);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Cập nhật thông tin thành công!";

                    var result = await response.Content.ReadFromJsonAsync<UpdateAvatarResponse>();
                    if (!string.IsNullOrEmpty(result?.newAvatarUrl))
                    {
                        HttpContext.Session.SetString("AvatarUrl", result.newAvatarUrl);
                        await UpdateClaims(Input.HoTen);
                    }
                }
                else
                {
                    ErrorMessage = $"Lỗi từ API: {await response.Content.ReadAsStringAsync()}";
                    return await OnGetAsync("Edit");
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
                return await OnGetAsync("Edit");
            }

            return RedirectToPage();
        }

        // (Hàm helper UpdateClaims và class UpdateAvatarResponse giữ nguyên)
        private async Task UpdateClaims(string newHoTen)
        {
            var user = User.Identity as ClaimsIdentity;
            if (user != null)
            {
                var oldClaim = user.FindFirst(ClaimTypes.GivenName);
                if (oldClaim != null) user.RemoveClaim(oldClaim);
                user.AddClaim(new Claim(ClaimTypes.GivenName, newHoTen));

                await HttpContext.SignOutAsync();
                await HttpContext.SignInAsync(new ClaimsPrincipal(user));
            }
        }
        private class UpdateAvatarResponse { public string? newAvatarUrl { get; set; } }
    }
}