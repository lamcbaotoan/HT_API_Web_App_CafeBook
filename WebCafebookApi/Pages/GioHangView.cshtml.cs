using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebCafebookApi.Services;
using System.Linq;

namespace WebCafebookApi.Pages
{
    public class GioHangViewPageModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public GioHangViewPageModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public GioHangViewModel Cart { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // SỬA LỖI CS0104: Chỉ định rõ namespace
            var sessionCart = HttpContext.Session.Get<List<CartItemDto>>(WebCafebookApi.Services.SessionExtensions.CartKey);
            if (sessionCart == null || !sessionCart.Any())
            {
                return Page();
            }

            // ... (Phần còn lại của OnGetAsync giữ nguyên) ...
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            var populatedItems = new List<GioHangItemViewModel>();

            foreach (var item in sessionCart)
            {
                try
                {
                    if (item.Loai == "Sach")
                    {
                        var sach = await httpClient.GetFromJsonAsync<SachChiTietDto>($"api/web/thuvien/{item.Id}");
                        if (sach != null)
                        {
                            populatedItems.Add(new GioHangItemViewModel
                            {
                                Id = item.Id,
                                Loai = item.Loai,
                                TenHienThi = sach.TieuDe,
                                HinhAnhUrl = sach.AnhBiaUrl,
                                DonGia = sach.GiaBia,
                                SoLuong = 1
                            });
                        }
                    }
                    else if (item.Loai == "SanPham")
                    {
                        var sp = await httpClient.GetFromJsonAsync<SanPhamChiTietDto>($"api/web/thucdon/{item.Id}");
                        if (sp != null)
                        {
                            populatedItems.Add(new GioHangItemViewModel
                            {
                                Id = item.Id,
                                Loai = item.Loai,
                                TenHienThi = sp.TenSanPham,
                                HinhAnhUrl = sp.HinhAnhUrl,
                                DonGia = sp.DonGia,
                                SoLuong = item.SoLuong
                            });
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ErrorMessage = "Lỗi khi tải thông tin giỏ hàng: " + ex.Message;
                }
            }
            Cart.Items = populatedItems;
            return Page();
        }

        public IActionResult OnPostRemove(int id, string loai)
        {
            // SỬA LỖI CS0104: Chỉ định rõ namespace
            var sessionCart = HttpContext.Session.Get<List<CartItemDto>>(WebCafebookApi.Services.SessionExtensions.CartKey);
            if (sessionCart != null)
            {
                var itemToRemove = sessionCart.FirstOrDefault(i => i.Id == id && i.Loai == loai);
                if (itemToRemove != null)
                {
                    sessionCart.Remove(itemToRemove);
                    // SỬA LỖI CS0104: Chỉ định rõ namespace
                    HttpContext.Session.Set(WebCafebookApi.Services.SessionExtensions.CartKey, sessionCart);
                }
            }
            return RedirectToPage();
        }

        public IActionResult OnPostUpdateQuantity(int id, string loai, int soLuong)
        {
            if (loai == "Sach" || soLuong <= 0)
            {
                return RedirectToPage();
            }

            // SỬA LỖI CS0104: Chỉ định rõ namespace
            var sessionCart = HttpContext.Session.Get<List<CartItemDto>>(WebCafebookApi.Services.SessionExtensions.CartKey);
            if (sessionCart != null)
            {
                var itemToUpdate = sessionCart.FirstOrDefault(i => i.Id == id && i.Loai == loai);
                if (itemToUpdate != null)
                {
                    itemToUpdate.SoLuong = soLuong;
                    // SỬA LỖI CS0104: Chỉ định rõ namespace
                    HttpContext.Session.Set(WebCafebookApi.Services.SessionExtensions.CartKey, sessionCart);
                }
            }
            return RedirectToPage();
        }
    }
}