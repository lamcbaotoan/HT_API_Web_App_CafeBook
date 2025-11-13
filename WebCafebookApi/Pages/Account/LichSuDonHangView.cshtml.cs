// TẠO TỆP MỚI: WebCafebookApi/Pages/Account/LichSuDonHangView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebCafebookApi.Pages.Account
{
    [Authorize(Roles = "KhachHang")]
    public class LichSuDonHangViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public List<LichSuDonHangWebDto> AllOrders { get; set; } = new List<LichSuDonHangWebDto>();
        public List<LichSuDonHangWebDto> FilteredOrders { get; set; } = new List<LichSuDonHangWebDto>();

        [TempData]
        public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "Tất cả";

        // Mảng này để render các tab
        public string[] StatusTabs { get; set; } = {
            "Tất cả", "Chờ xác nhận", "Chờ lấy hàng",
            "Đang giao", "Hoàn thành", "Đã Hủy" 
            // "Trả Hàng" sẽ được thêm khi có nghiệp vụ
        };

        public LichSuDonHangViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                var response = await httpClient.GetFromJsonAsync<List<LichSuDonHangWebDto>>($"/api/web/profile/order-history/{userId}");
                if (response != null)
                {
                    AllOrders = response;
                }

                // Lọc danh sách đơn hàng dựa trên tab
                FilterOrders();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải lịch sử đơn hàng: {ex.Message}";
            }
            return Page();
        }

        private void FilterOrders()
        {
            if (StatusFilter == "Tất cả")
            {
                FilteredOrders = AllOrders;
                return;
            }

            if (StatusFilter == "Đã Hủy")
            {
                // Đã hủy là trạng thái thanh toán
                FilteredOrders = AllOrders.Where(o => o.TrangThaiThanhToan == "Đã hủy").ToList();
            }
            else
            {
                // Các trạng thái còn lại là TrangThaiGiaoHang
                // (Và phải đảm bảo đơn đó chưa bị hủy)
                FilteredOrders = AllOrders.Where(o =>
                    o.TrangThaiGiaoHang == StatusFilter &&
                    o.TrangThaiThanhToan != "Đã hủy")
                    .ToList();
            }
        }
    }
}