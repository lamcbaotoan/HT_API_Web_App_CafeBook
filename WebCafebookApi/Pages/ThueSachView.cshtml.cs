using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Headers; // Cần nếu dùng JWT
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication; // Cần để lấy token
using System.Security.Claims;

namespace WebCafebookApi.Pages
{
    public class ThueSachViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; } // Id Sách từ URL

        public SachChiTietDto? Sach { get; set; }
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? FailureMessage { get; set; }

        public ThueSachViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Tải thông tin chi tiết sách (dùng API public)
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            // SỬA LỖI 401: Dùng "ApiClient"
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                // Gọi API lấy chi tiết sách (từ ThuVienSachController)
                Sach = await httpClient.GetFromJsonAsync<SachChiTietDto>($"http://localhost:5166/api/web/thuvien/{Id}");
                if (Sach == null)
                {
                    ErrorMessage = "Không tìm thấy sách.";
                    return Page();
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Không thể tải chi tiết sách. Đảm bảo API đang chạy. Lỗi: {ex.Message}";
            }
            return Page();
        }

        /// <summary>
        /// Xử lý khi nhấn nút "Thuê Sách" (dùng API private)
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Kiểm tra đăng nhập
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                // Nếu chưa, chuyển đến trang Đăng nhập và yêu cầu quay lại
                return RedirectToPage("/Account/Login", new { returnUrl = $"/ThueSachView/{Id}" });
            }

            // SỬA LỖI 401: Dùng "ApiClient" (đã tự động đính kèm token)
            var httpClient = _httpClientFactory.CreateClient("ApiClient");

            // 2. (Không cần đính kèm token thủ công nữa)

            try
            {
                // 3. Gọi API (ThueSachWebController)
                var requestDto = new ThueSachRequestDto { IdSach = this.Id };
                var response = await httpClient.PostAsJsonAsync("http://localhost:5166/api/web/thuesach/create", requestDto);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ThueSachResponseDto>();
                    SuccessMessage = $"Thuê sách '{result?.TenSach}' thành công! Hạn trả: {result?.NgayHenTra:dd/MM/yyyy}.";
                }
                else
                {
                    // Đọc lỗi từ API
                    var error = await response.Content.ReadAsStringAsync();
                    FailureMessage = $"Thuê sách thất bại: {error}";
                }
            }
            catch (System.Exception ex)
            {
                FailureMessage = $"Lỗi hệ thống: {ex.Message}";
            }

            // 4. Tải lại trang để hiển thị thông báo
            return RedirectToPage(new { id = this.Id });
        }
    }
}