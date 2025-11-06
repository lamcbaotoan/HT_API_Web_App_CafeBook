// Tập tin: WebCafebookApi/Pages/ChiTietSachView.cshtml.cs
using CafebookModel.Model.ModelWeb; // For SachChiTietDto
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic; // For List
using System.ComponentModel.DataAnnotations; // For InputModel
using System.Net.Http.Json;
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

        // Dùng cho form thuê sách (mặc dù chưa xử lý POST)
        [BindProperty]
        public RentInputModel Input { get; set; } = new();
        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? RentErrorMessage { get; set; }

        // Dùng cho placeholder đánh giá
        public List<ReviewPlaceholder> DanhGiaList { get; set; } = new();

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
                Sach = await httpClient.GetFromJsonAsync<SachChiTietDto>($"http://localhost:5166/api/web/thuvien/{Id}");
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

        // Lớp Input cho Form thuê
        public class RentInputModel
        {
            [Required]
            public int IdSach { get; set; }
            [Required(ErrorMessage = "Vui lòng chọn ngày hẹn trả")]
            [Display(Name = "Ngày hẹn trả")]
            public System.DateTime NgayHenTra { get; set; }
        }

        // Lớp Placeholder cho Đánh giá
        public class ReviewPlaceholder
        {
            public string TenNguoiBinhLuan { get; set; } = string.Empty;
            public int Rating { get; set; }
            public string BinhLuan { get; set; } = string.Empty;
            public System.DateTime NgayDanhGia { get; set; }
        }
    }
}