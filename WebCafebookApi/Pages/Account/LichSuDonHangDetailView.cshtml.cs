// TỆP: WebCafebookApi/Pages/Account/LichSuDonHangDetailView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
// Thêm 2 using này để xử lý AuthenticationHeaderValue
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

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

        // ==================================================
        // ===== VÙNG CODE ĐÃ SỬA LỖI _apiClient =====
        // ==================================================
        public async Task<bool> KiemTraDaDanhGia(int idHoaDon, int? idSanPham, int? idSach)
        {
            // 1. Lấy HttpClient từ Factory (giống như OnGetAsync)
            var httpClient = _httpClientFactory.CreateClient("ApiClient");

            // 2. Lấy token từ Session
            var accessToken = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return true; // Không có token, không thể kiểm tra, tạm ẩn nút
            }

            // 3. Gắn token vào header của request này
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                // 4. Gọi API kiểm tra bằng httpClient
                // (Giả định API controller của bạn là 'DanhGiaWebController' và route là 'api/danhgia/kiemtra')
                var daDanhGia = await httpClient.GetFromJsonAsync<bool>($"api/danhgia/kiemtra?idHoaDon={idHoaDon}&idSanPham={idSanPham}&idSach={idSach}");

                // 5. Xóa header sau khi dùng xong
                httpClient.DefaultRequestHeaders.Authorization = null;
                return daDanhGia;
            }
            catch (Exception)
            {
                httpClient.DefaultRequestHeaders.Authorization = null;
                return true; // Giả định là ĐÃ đánh giá nếu có lỗi, để tránh cho người dùng đánh giá lại
            }
        }
    }
}