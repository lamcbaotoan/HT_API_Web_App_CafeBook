using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Claims;

namespace WebCafebookApi.Pages
{
    public class DatBanViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DatBanViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // --- MODEL BINDING ---
        [BindProperty]
        public SearchModel Search { get; set; } = new();

        [BindProperty]
        public BookingInfoModel Booking { get; set; } = new();

        public List<BanTrongDto> AvailableTables { get; set; } = new();

        public bool IsSearched { get; set; } = false;
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        // --- NESTED MODELS ---
        public class SearchModel
        {
            [Required(ErrorMessage = "Vui lòng chọn ngày")]
            [DataType(DataType.Date)]
            public DateTime Date { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "Vui lòng chọn giờ")]
            public string Time { get; set; } = "18:00"; // Giờ mặc định

            [Required, Range(1, 50, ErrorMessage = "Số người từ 1-50")]
            public int People { get; set; } = 2;
        }

        public class BookingInfoModel
        {
            public int SelectedTableId { get; set; }
            public string? SelectedTableNumber { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập họ tên")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập SĐT")]
            [Phone(ErrorMessage = "SĐT không hợp lệ")]
            public string Phone { get; set; } = string.Empty;

            public string? Note { get; set; }
        }

        // --- HANDLERS ---
        public void OnGet()
        {
            // Nếu đã đăng nhập, điền sẵn thông tin
            if (User.Identity?.IsAuthenticated == true)
            {
                Booking.FullName = User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity.Name ?? "";
                Booking.Phone = User.FindFirstValue(ClaimTypes.MobilePhone) ?? "";
            }
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            if (!TimeSpan.TryParse(Search.Time, out TimeSpan time))
            {
                ModelState.AddModelError("Search.Time", "Giờ không hợp lệ");
                return Page();
            }

            var req = new TimBanRequestDto
            {
                NgayDat = Search.Date,
                GioDat = time,
                SoNguoi = Search.People
            };

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync("http://localhost:5166/api/web/datban/tim-ban", req);

            if (response.IsSuccessStatusCode)
            {
                AvailableTables = await response.Content.ReadFromJsonAsync<List<BanTrongDto>>() ?? new List<BanTrongDto>();
                IsSearched = true;
            }
            else
            {
                ErrorMessage = await response.Content.ReadAsStringAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostBookAsync()
        {
            // Parse lại giờ từ form search (vì form booking dùng chung dữ liệu thời gian)
            TimeSpan.TryParse(Search.Time, out TimeSpan time);

            var req = new DatBanWebRequestDto
            {
                IdBan = Booking.SelectedTableId,
                NgayDat = Search.Date,
                GioDat = time,
                SoLuongKhach = Search.People,
                HoTen = Booking.FullName,
                SoDienThoai = Booking.Phone,
                GhiChu = Booking.Note
            };

            // Gửi kèm cookie xác thực để API biết là ai (nếu có)
            var client = _httpClientFactory.CreateClient("WebClient"); // Cần cấu hình Named Client nếu muốn truyền cookie, hoặc thủ công:
            // (Đơn giản nhất là truyền thẳng thông tin user qua DTO nếu API không dùng chung cookie auth scheme)
            // Ở đây API DatBanWebController đã có logic check User.Identity,
            // nhưng cần đảm bảo Web App và API chia sẻ cookie hoặc dùng token.
            // NẾU KHÔNG CHIA SẺ COOKIE: API sẽ luôn thấy là khách vãng lai.
            // Để đơn giản cho demo này, ta cứ gửi thông tin từ form.

            var response = await client.PostAsJsonAsync("http://localhost:5166/api/web/datban/tao-yeu-cau", req);

            if (response.IsSuccessStatusCode)
            {
                SuccessMessage = "Đặt bàn thành công! Vui lòng chờ nhân viên xác nhận.";
                IsSearched = false; // Reset trạng thái tìm kiếm
                AvailableTables.Clear();
            }
            else
            {
                ErrorMessage = await response.Content.ReadAsStringAsync();
                IsSearched = true; // Giữ lại kết quả tìm kiếm để chọn bàn khác
                // Cần gọi lại API tìm bàn nếu muốn chắc chắn danh sách mới nhất
            }

            return Page();
        }
    }
}