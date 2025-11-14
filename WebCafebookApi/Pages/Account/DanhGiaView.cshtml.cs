using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WebCafebookApi.Pages.Account
{
    [Authorize]
    public class DanhGiaViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DanhGiaViewModel(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        // Dùng để nhận dữ liệu từ form
        [BindProperty]
        public TaoDanhGiaDto DanhGiaInput { get; set; } = new TaoDanhGiaDto();

        // Dùng để nhận file ảnh upload
        [BindProperty]
        public IFormFile? HinhAnhFile { get; set; }

        // --- CẢI TIẾN: Hiển thị danh sách sản phẩm ---
        // Sử dụng DTO từ CafebookModel
        public List<SanPhamChoDanhGiaDto> SanPhamsDeDanhGia { get; set; } = new List<SanPhamChoDanhGiaDto>();

        [BindProperty(SupportsGet = true)]
        public int IdHoaDonHienTai { get; set; }
        // --- KẾT THÚC CẢI TIẾN ---


        private async Task<HttpClient> GetClientAsync()
        {
            var client = _httpClientFactory.CreateClient("CafebookApi");
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["jwtToken"];
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        // OnGet để lấy danh sách sản phẩm của đơn hàng
        public async Task<IActionResult> OnGetAsync([Required] int idHoaDon)
        {
            IdHoaDonHienTai = idHoaDon; // Gán ID từ URL
            var client = await GetClientAsync();

            try
            {
                // Gọi API mới (api/web/danhgia/cho-danh-gia/{idHoaDon})
                var response = await client.GetFromJsonAsync<List<SanPhamChoDanhGiaDto>>(
                    $"api/web/danhgia/cho-danh-gia/{idHoaDon}");

                if (response != null)
                {
                    SanPhamsDeDanhGia = response;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không thể tải danh sách sản phẩm. " + ex.Message;
                SanPhamsDeDanhGia = new List<SanPhamChoDanhGiaDto>();
            }

            return Page();
        }

        // OnPost để gửi MỘT đánh giá
        public async Task<IActionResult> OnPostAsync()
        {
            // Gán lại IdHoaDonHienTai vì nó không được post về
            // (Nó được post về qua DanhGiaInput.idHoaDon)
            IdHoaDonHienTai = DanhGiaInput.idHoaDon;

            if (DanhGiaInput.SoSao < 1 || DanhGiaInput.SoSao > 5)
            {
                ModelState.AddModelError("DanhGiaInput.SoSao", "Bạn phải chọn số sao.");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Thông tin đánh giá không hợp lệ. Vui lòng chọn số sao.";
                // Tải lại danh sách
                await OnGetAsync(IdHoaDonHienTai);
                return Page();
            }

            var client = await GetClientAsync();

            using var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(DanhGiaInput.idHoaDon.ToString()), "idHoaDon");
            formData.Add(new StringContent(DanhGiaInput.SoSao.ToString()), "SoSao");

            // Chỉ xử lý idSanPham
            formData.Add(new StringContent(DanhGiaInput.idSanPham.ToString()), "idSanPham");

            if (!string.IsNullOrEmpty(DanhGiaInput.BinhLuan))
            {
                formData.Add(new StringContent(DanhGiaInput.BinhLuan), "BinhLuan");
            }

            if (HinhAnhFile != null && HinhAnhFile.Length > 0)
            {
                var fileContent = new StreamContent(HinhAnhFile.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(HinhAnhFile.ContentType);
                formData.Add(fileContent, "hinhAnhFile", HinhAnhFile.FileName);
            }

            try
            {
                var response = await client.PostAsync("api/web/danhgia", formData);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Gửi đánh giá thành công!";
                    // Chuyển hướng về chính trang này (để tải lại danh sách đã cập nhật)
                    return RedirectToPage("/Account/DanhGiaView", new { idHoaDon = IdHoaDonHienTai });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi khi gửi đánh giá: {errorContent}";
                    await OnGetAsync(IdHoaDonHienTai); // Tải lại danh sách
                    return Page();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                await OnGetAsync(IdHoaDonHienTai); // Tải lại danh sách
                return Page();
            }
        }
    }
}