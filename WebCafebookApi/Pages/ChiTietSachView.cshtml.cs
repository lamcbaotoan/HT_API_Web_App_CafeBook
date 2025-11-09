// Tập tin: WebCafebookApi/Pages/ChiTietSachView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization; // Thêm
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http; // Thêm
using System.Net.Http.Json;
using System.Security.Claims; // Thêm
using System.Threading.Tasks;

namespace WebCafebookApi.Pages
{
    public class ChiTietSachViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ChiTietSachViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public SachChiTietDto? Sach { get; set; }
        public string? ErrorMessage { get; set; }

        [BindProperty]
        public RentInputModel Input { get; set; } = new();
        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? RentErrorMessage { get; set; }

        public List<ReviewPlaceholder> DanhGiaList { get; set; } = new();

        // Helper lấy ID người dùng
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);
            return userId;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id <= 0)
            {
                ErrorMessage = "ID sách không hợp lệ.";
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                // Gọi API đã sửa (trả về List<TacGiaDto>...)
                Sach = await httpClient.GetFromJsonAsync<SachChiTietDto>($"http://localhost:5166/api/web/thuvien/{Id}");
                if (Sach == null)
                {
                    ErrorMessage = "Không tìm thấy cuốn sách bạn yêu cầu.";
                    return Page();
                }

                Input.IdSach = Id;
                Input.NgayHenTra = System.DateTime.Today.AddDays(7);
            }
            catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ErrorMessage = "Không tìm thấy cuốn sách bạn yêu cầu.";
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi khi tải chi tiết: {ex.Message}";
            }

            return Page();
        }

        // === NÂNG CẤP: XỬ LÝ POST THUÊ SÁCH ===
        [Authorize(Roles = "KhachHang")] // Yêu cầu đăng nhập
        public async Task<IActionResult> OnPostAsync(string handler)
        {
            if (handler != "Rent") return NotFound();

            var userId = GetCurrentUserId();
            if (userId == 0) return Challenge(); // Lỗi nếu không tìm thấy ID

            if (!ModelState.IsValid)
            {
                // Nếu model không hợp lệ, tải lại trang
                return await OnGetAsync();
            }

            var httpClient = _httpClientFactory.CreateClient();
            var request = new SachThueRequestDto
            {
                IdSach = Input.IdSach,
                IdKhachHang = userId,
                NgayHenTra = Input.NgayHenTra
            };

            try
            {
                // Giả định API endpoint mới
                var response = await httpClient.PostAsJsonAsync("http://localhost:5166/api/web/thuvien/rent", request);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Thuê sách thành công! Vui lòng đến quầy để nhận sách.";
                }
                else
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    RentErrorMessage = $"Lỗi khi thuê sách: {errorMsg}";
                }
            }
            catch (System.Exception ex)
            {
                RentErrorMessage = $"Lỗi hệ thống: {ex.Message}";
            }

            return RedirectToPage(new { id = Input.IdSach });
        }
        // ===================================

        // Lớp Input cho Form thuê
        public class RentInputModel
        {
            [Required]
            public int IdSach { get; set; }
            [Required(ErrorMessage = "Vui lòng chọn ngày hẹn trả")]
            [Display(Name = "Ngày hẹn trả")]
            [DataType(DataType.Date)]
            public System.DateTime NgayHenTra { get; set; }
        }

        // DTO gửi yêu cầu thuê (có thể đặt ở file riêng)
        public class SachThueRequestDto
        {
            public int IdSach { get; set; }
            public int IdKhachHang { get; set; }
            public System.DateTime NgayHenTra { get; set; }
        }

        // Lớp Placeholder cho Đánh giá
        public class ReviewPlaceholder { /* ... */ }
    }
}