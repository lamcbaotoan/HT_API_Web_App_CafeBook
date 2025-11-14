// Tập tin: WebCafebookApi/Pages/ChiTietSanPhamView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebCafebookApi.Services;
using System.Collections.Generic;
using System.Linq;

namespace WebCafebookApi.Pages
{
    public class ChiTietSanPhamViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ChiTietSanPhamViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public int SoLuong { get; set; } = 1;

        public SanPhamChiTietDto? SanPham { get; set; }
        public string? ErrorMessage { get; set; }

        public List<DanhGiaWebDto> DanhSachDanhGia { get; set; } = new List<DanhGiaWebDto>();
        public double SaoTrungBinh { get; set; } = 0;
        public int TongSoDanhGia { get; set; } = 0;

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id <= 0)
            {
                ErrorMessage = "ID sản phẩm không hợp lệ.";
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                SanPham = await httpClient.GetFromJsonAsync<SanPhamChiTietDto>($"api/web/thucdon/{Id}");

                // === SỬA LỖI CS0246: Đổi tên DTO cho đúng ===
                DanhSachDanhGia = await httpClient.GetFromJsonAsync<List<DanhGiaWebDto>>($"api/danhgia/sanpham/{Id}");

                // Tính toán thông tin tóm tắt đánh giá
                if (DanhSachDanhGia.Any())
                {
                    TongSoDanhGia = DanhSachDanhGia.Count;
                    SaoTrungBinh = DanhSachDanhGia.Average(d => d.SoSao);
                }
            }
            catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ErrorMessage = "Không tìm thấy sản phẩm bạn yêu cầu.";
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi khi tải chi tiết: {ex.Message}";
            }

            return Page();
        }


        // === SỬA HÀM XỬ LÝ GIỎ HÀNG (XÓA 'Loai') ===
        public IActionResult OnPostAddToCart(int idSanPham)
        {
            var cart = HttpContext.Session.Get<List<CartItemDto>>(WebCafebookApi.Services.SessionExtensions.CartKey) ?? new List<CartItemDto>();

            // SỬA LỖI CS1061: Xóa 'Loai' khỏi logic tìm kiếm
            var existingItem = cart.FirstOrDefault(i => i.Id == idSanPham);

            if (existingItem != null)
            {
                existingItem.SoLuong += this.SoLuong;
                TempData["CartMessage"] = $"Đã cập nhật số lượng (tổng: {existingItem.SoLuong}).";
            }
            else
            {
                // SỬA LỖI CS0117: Xóa 'Loai' khỏi đối tượng
                cart.Add(new CartItemDto { Id = idSanPham, SoLuong = this.SoLuong });
                TempData["CartMessage"] = "Đã thêm sản phẩm vào giỏ!";
            }

            HttpContext.Session.Set(WebCafebookApi.Services.SessionExtensions.CartKey, cart);
            return RedirectToPage(new { id = idSanPham });
        }
    }
}