using CafebookModel.Model.ModelWeb.QuanLy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace WebCafebookApi.Pages.employee
{
    [Authorize(Roles = "Quản trị viên, Phục vụ, Thu ngân, Pha chế, Bếp, Quản lý")]
    public class SoDoBanViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public int NhanVienId { get; set; }
        public string? ErrorMessage { get; set; }
        public List<KhuVucDto> KhuVucList { get; set; } = new();

        public SoDoBanViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var idNhanVienStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(idNhanVienStr, out var id);
            NhanVienId = id;

            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                var response = await httpClient.GetFromJsonAsync<List<KhuVucDto>>("http://localhost:5166/api/web/quanly/sodoban");
                if (response != null)
                {
                    KhuVucList = response;
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi tải Sơ đồ bàn: {ex.Message}";
            }

            return Page();
        }
    }
}