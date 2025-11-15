using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Net.Http.Json;

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

        [BindProperty]
        public TaoDanhGiaDto DanhGiaInput { get; set; } = new TaoDanhGiaDto();

        [BindProperty]
        public IFormFile? HinhAnhFile { get; set; }

        public List<SanPhamChoDanhGiaDto> SanPhamsDeDanhGia { get; set; } = new List<SanPhamChoDanhGiaDto>();

        [BindProperty(SupportsGet = true)]
        public int IdHoaDonHienTai { get; set; }

        public async Task<IActionResult> OnGetAsync(int idHoaDon)
        {
            IdHoaDonHienTai = idHoaDon;
            var client = _httpClientFactory.CreateClient("ApiClient");

            // <<< SỬA LỖI 1: Sửa "JWToken" thành "JwtToken"
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");

            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn.";
                // <<< SỬA LỖI 2: Sửa "/Account/LoginView" thành "/Account/Login"
                return RedirectToPage("/Account/Login");
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var apiUrl = $"api/web/danhgia/cho-danh-gia/{idHoaDon}";
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    SanPhamsDeDanhGia = await response.Content.ReadFromJsonAsync<List<SanPhamChoDanhGiaDto>>() ?? new List<SanPhamChoDanhGiaDto>();

                    if (!SanPhamsDeDanhGia.Any())
                    {
                        TempData["InfoMessage"] = "Không tìm thấy sản phẩm nào trong đơn hàng này hoặc đơn hàng chưa hoàn thành.";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Không thể tải danh sách sản phẩm: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống khi tải trang: {ex.Message}";
            }

            return Page();
        }


        public async Task<IActionResult> OnPostSubmitReviewAsync()
        {
            IdHoaDonHienTai = DanhGiaInput.idHoaDon;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                await OnGetAsync(IdHoaDonHienTai);
                return Page();
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            // <<< SỬA LỖI 1: Sửa "JWToken" thành "JwtToken"
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");

            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn.";
                // <<< SỬA LỖI 2: Sửa "/Account/LoginView" thành "/Account/Login"
                return RedirectToPage("/Account/Login");
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(DanhGiaInput.idHoaDon.ToString()), "idHoaDon");
            formData.Add(new StringContent(DanhGiaInput.idSanPham.ToString()), "idSanPham");
            formData.Add(new StringContent(DanhGiaInput.SoSao.ToString()), "SoSao");

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
                    return RedirectToPage("/Account/DanhGiaView", new { idHoaDon = IdHoaDonHienTai });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi khi gửi đánh giá: {errorContent}";
                    await OnGetAsync(IdHoaDonHienTai);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                await OnGetAsync(IdHoaDonHienTai);
                return Page();
            }
        }
    }
}