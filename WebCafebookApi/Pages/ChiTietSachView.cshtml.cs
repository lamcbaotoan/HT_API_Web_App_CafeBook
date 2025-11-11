// Tập tin: WebCafebookApi/Pages/ChiTietSachView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebCafebookApi.Services; // Thêm using này
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace WebCafebookApi.Pages
{
    public class ChiTietSachViewModel : PageModel
    {
        // ... (Constructor và OnGetAsync giữ nguyên) ...
        private readonly IHttpClientFactory _httpClientFactory;
        public ChiTietSachViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public SachChiTietDto? Sach { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id <= 0)
            {
                ErrorMessage = "ID sách không hợp lệ.";
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                Sach = await httpClient.GetFromJsonAsync<SachChiTietDto>($"api/web/thuvien/{Id}");
                if (Sach == null)
                {
                    ErrorMessage = "Không tìm thấy cuốn sách bạn yêu cầu.";
                    return Page();
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi khi tải chi tiết: {ex.Message}";
            }
            return Page();
        }


        // === CẬP NHẬT HÀM XỬ LÝ GIỎ HÀNG ===
        public IActionResult OnPostAddToCart(int idSach)
        {
            // SỬA LỖI CS0104: Chỉ định rõ namespace
            var cart = HttpContext.Session.Get<List<CartItemDto>>(WebCafebookApi.Services.SessionExtensions.CartKey) ?? new List<CartItemDto>();

            var existingItem = cart.FirstOrDefault(i => i.Id == idSach && i.Loai == "Sach");

            if (existingItem == null)
            {
                cart.Add(new CartItemDto { Id = idSach, Loai = "Sach", SoLuong = 1 });
                // SỬA LỖI CS0104: Chỉ định rõ namespace
                HttpContext.Session.Set(WebCafebookApi.Services.SessionExtensions.CartKey, cart);
                TempData["CartMessage"] = "Đã thêm sách vào giỏ thuê!";
            }
            else
            {
                TempData["CartMessage"] = "Sách này đã có trong giỏ.";
            }

            return RedirectToPage(new { id = idSach });
        }
    }
}