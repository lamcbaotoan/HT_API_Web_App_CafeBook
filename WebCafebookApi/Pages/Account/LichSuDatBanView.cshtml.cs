// Tập tin: WebCafebookApi/Pages/Account/LichSuDatBanView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebCafebookApi.Pages.Account
{
    [Authorize(Roles = "KhachHang")]
    public class LichSuDatBanViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public List<LichSuDatBanDto> Bookings { get; set; } = new List<LichSuDatBanDto>();

        [TempData]
        public string? ErrorMessage { get; set; }

        public LichSuDatBanViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);
            return userId;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Challenge();

            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                var response = await httpClient.GetAsync($"http://localhost:5166/api/web/profile/booking-history/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    Bookings = await response.Content.ReadFromJsonAsync<List<LichSuDatBanDto>>() ?? new List<LichSuDatBanDto>();
                }
                else
                {
                    ErrorMessage = "Không thể tải lịch sử đặt bàn.";
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi kết nối: {ex.Message}";
            }

            return Page();
        }
    }
}