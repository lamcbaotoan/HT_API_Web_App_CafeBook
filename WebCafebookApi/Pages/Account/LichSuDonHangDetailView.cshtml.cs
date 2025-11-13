// TẠO TỆP MỚI: WebCafebookApi/Pages/Account/LichSuDonHangDetailView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace WebCafebookApi.Pages.Account
{
    [Authorize(Roles = "KhachHang")]
    public class LichSuDonHangDetailViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LichSuDonHangDetailViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; } // IdHoaDon

        public DonHangChiTietWebDto? OrderDetails { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id == 0)
            {
                ErrorMessage = "Mã đơn hàng không hợp lệ.";
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                // Gọi API mới trong KhachHangProfileController
                OrderDetails = await httpClient.GetFromJsonAsync<DonHangChiTietWebDto>($"/api/web/profile/order-detail/{Id}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ErrorMessage = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn hàng này.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải chi tiết đơn hàng: {ex.Message}";
            }

            return Page();
        }
    }
}