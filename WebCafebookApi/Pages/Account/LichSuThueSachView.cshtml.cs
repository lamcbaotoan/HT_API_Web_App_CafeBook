// T?p tin: WebCafebookApi/Pages/Account/LichSuThueSachView.cshtml.cs
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
    public class LichSuThueSachViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public List<LichSuPhieuThueDto> Rentals { get; set; } = new List<LichSuPhieuThueDto>();

        [TempData]
        public string? ErrorMessage { get; set; }

        public LichSuThueSachViewModel(IHttpClientFactory httpClientFactory)
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
                var response = await httpClient.GetAsync($"http://localhost:5166/api/web/profile/rental-history/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    Rentals = await response.Content.ReadFromJsonAsync<List<LichSuPhieuThueDto>>() ?? new List<LichSuPhieuThueDto>();
                }
                else
                {
                    ErrorMessage = "Không th? t?i l?ch s? thuê sách.";
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"L?i k?t n?i: {ex.Message}";
            }

            return Page();
        }
    }
}