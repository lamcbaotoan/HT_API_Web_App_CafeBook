// Tập tin: WebCafebookApi/Pages/Account/ThanhToanView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using WebCafebookApi.Services;
using System.Linq;
using System.Collections.Generic;

namespace WebCafebookApi.Pages.Account
{
    [Authorize(Roles = "KhachHang")]
    public class ThanhToanViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ThanhToanViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public ThanhToanLoadDto PageData { get; set; } = new();

        [BindProperty]
        public ThanhToanSubmitDto Input { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }

        private HttpClient GetApiClient()
        {
            // ApiClient (từ Program.cs) đã tự động đính kèm JWT Token
            return _httpClientFactory.CreateClient("ApiClient");
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var httpClient = GetApiClient();
            try
            {
                // 1. Tải dữ liệu API (Khách hàng, Khuyến mãi, Tỷ lệ điểm)
                var loadedData = await httpClient.GetFromJsonAsync<ThanhToanLoadDto>("api/web/thanhtoan/load");

                if (loadedData == null)
                {
                    ErrorMessage = "Không thể tải dữ liệu thanh toán.";
                    return Page();
                }
                PageData = loadedData;

                // 2. Tải giỏ hàng từ SESSION (của Frontend)
                var sessionCart = HttpContext.Session.Get<List<CartItemDto>>(WebCafebookApi.Services.SessionExtensions.CartKey)
                                  ?? new List<CartItemDto>();

                // 3. Lấy thông tin chi tiết cho giỏ hàng
                var sanPhamItems = new List<GioHangItemViewModel>();
                // SỬA: Xóa List<GioHangItemViewModel> sachItems

                var publicClient = _httpClientFactory.CreateClient(); // Client không auth

                foreach (var item in sessionCart)
                {
                    // =======================================
                    // === SỬA LỖI CS1061 TẠI ĐÂY ===
                    // Xóa toàn bộ logic if/else (item.Loai), vì giỏ hàng chỉ còn IdSanPham
                    try
                    {
                        var sp = await publicClient.GetFromJsonAsync<SanPhamChiTietDto>($"http://localhost:5166/api/web/thucdon/{item.Id}");
                        if (sp != null)
                        {
                            sanPhamItems.Add(new GioHangItemViewModel
                            {
                                Id = item.Id,
                                // SỬA: Xóa 'Loai'
                                TenHienThi = sp.TenSanPham,
                                HinhAnhUrl = sp.HinhAnhUrl,
                                DonGia = sp.DonGia,
                                SoLuong = item.SoLuong
                            });
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ErrorMessage = "Lỗi khi tải thông tin giỏ hàng: " + ex.Message;
                    }
                    // =======================================
                }

                // 4. Gán giỏ hàng đã xử lý vào PageData
                PageData.SanPhamItems = sanPhamItems;
                PageData.SachItems = new List<GioHangItemViewModel>(); // SỬA: Khởi tạo rỗng
                PageData.TongTienHang = sanPhamItems.Sum(i => i.ThanhTien);


                // 5. Xử lý chuyển hướng nếu giỏ hàng không hợp lệ
                // SỬA: Xóa logic kiểm tra sách
                if (PageData.SanPhamItems.Count == 0)
                {
                    return RedirectToPage("/GioHangView");
                }

                // 6. Điền thông tin mặc định cho form Input
                Input.HoTen = PageData.KhachHang.HoTen;
                Input.SoDienThoai = PageData.KhachHang.SoDienThoai;
                Input.Email = PageData.KhachHang.Email;
                Input.DiaChiGiaoHang = PageData.KhachHang.DiaChi;
                Input.PhuongThucThanhToan = "COD";
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi tải trang: {ex.Message}";
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var loadTask = OnGetAsync(); // Tải lại dữ liệu nền

            if (!ModelState.IsValid)
            {
                await loadTask;
                return Page();
            }

            var httpClient = GetApiClient();
            try
            {
                var sessionCart = HttpContext.Session.Get<List<CartItemDto>>(WebCafebookApi.Services.SessionExtensions.CartKey)
                                  ?? new List<CartItemDto>();
                Input.ItemsToPurchase = sessionCart; // Gán giỏ hàng vào DTO

                var response = await httpClient.PostAsJsonAsync("api/web/thanhtoan/submit", Input);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ThanhToanResponseDto>();
                    SuccessMessage = result?.Message ?? "Đặt hàng thành công!";

                    // =======================================
                    // === SỬA LỖI CS1061 TẠI ĐÂY ===
                    // Xóa giỏ hàng hoàn toàn (thay vì lọc sách)
                    HttpContext.Session.Remove(WebCafebookApi.Services.SessionExtensions.CartKey);
                    // =======================================

                    // =======================================
                    // === YÊU CẦU MỚI: Chuyển hướng ===
                    // =======================================
                    return RedirectToPage("/Account/ThanhToanThanhCongView", new { id = result.IdHoaDonMoi });
                }
                else
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<ThanhToanResponseDto>();
                    ErrorMessage = errorResult?.Message ?? "Đã xảy ra lỗi khi đặt hàng.";
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi hệ thống: {ex.Message}";
            }

            await loadTask;
            return Page();
        }
    }
}