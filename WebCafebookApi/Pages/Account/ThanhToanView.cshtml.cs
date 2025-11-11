using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebCafebookApi.Services;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
// SỬA LỖI CS0246: Xóa dòng 'using CafebookModel.Model.ModelApp;'

namespace WebCafebookApi.Pages
{
    [Authorize]
    public class ThanhToanViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly EmailService _emailService;

        public ThanhToanViewModel(IHttpClientFactory httpClientFactory, EmailService emailService)
        {
            _httpClientFactory = httpClientFactory;
            _emailService = emailService;
        }

        public CafebookModel.Model.ModelWeb.ThanhToanViewModel CheckoutInfo { get; set; } = new();

        [BindProperty]
        public ThanhToanInputModel Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var sessionCart = HttpContext.Session.Get<List<CartItemDto>>(WebCafebookApi.Services.SessionExtensions.CartKey);
            if (sessionCart == null || !sessionCart.Any())
            {
                TempData["CartError"] = "Giỏ hàng của bạn trống.";
                return RedirectToPage("/GioHangView");
            }

            var httpClient = _httpClientFactory.CreateClient("ApiClient");

            var populatedItems = await GetCartDetails(httpClient, sessionCart);
            CheckoutInfo.Items = populatedItems;
            CheckoutInfo.TongTien = populatedItems.Sum(i => i.ThanhTien);

            // SỬA LỖI CS0246: XÓA BỎ HOÀN TOÀN KHỐI try-catch BÊN DƯỚI
            // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // try
            // {
            //     var userInfo = await httpClient.GetFromJsonAsync<UserInfoDto>($"api/shared/users/{userId}");
            //     ...
            // }
            // catch (System.Exception) { /* Bỏ qua */ }

            return Page();
        }

        // ... (Hàm OnPostAsync và GetCartDetails giữ nguyên) ...
        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Tải lại thông tin giỏ hàng để hiển thị nếu có lỗi
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            var sessionCart = HttpContext.Session.Get<List<CartItemDto>>(WebCafebookApi.Services.SessionExtensions.CartKey);

            if (sessionCart == null || !sessionCart.Any())
            {
                return RedirectToPage("/GioHangView");
            }

            // Tải chi tiết item (để dùng cho email và hiển thị lại nếu lỗi)
            var populatedItems = await GetCartDetails(httpClient, sessionCart);
            CheckoutInfo.Items = populatedItems;
            CheckoutInfo.TongTien = populatedItems.Sum(i => i.ThanhTien);

            if (!ModelState.IsValid)
            {
                return Page(); // Trả về trang với thông tin giỏ hàng đã tải
            }

            // 2. Chuẩn bị DTO để gửi lên API
            var requestDto = new DatHangRequestDto
            {
                ThongTinNhanHang = this.Input,
                Items = sessionCart
            };

            try
            {
                // 3. Gọi API Backend để tạo đơn hàng
                var response = await httpClient.PostAsJsonAsync("api/web/thanhtoan/dat-hang", requestDto);

                if (response.IsSuccessStatusCode)
                {
                    // 4. Xóa giỏ hàng
                    HttpContext.Session.Remove(WebCafebookApi.Services.SessionExtensions.CartKey);

                    // 5. GỬI EMAIL HÓA ĐƠN CHI TIẾT
                    var userEmail = User.FindFirstValue(ClaimTypes.Email);
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        var subject = "Xác nhận đơn hàng Cafebook";

                        // Truyền CheckoutInfo (đã có item) và Input (có tên, địa chỉ)
                        var body = BuildInvoiceEmailBody(Input, CheckoutInfo);

                        await _emailService.SendEmailAsync(userEmail, subject, body);
                    }

                    TempData["OrderSuccess"] = "Bạn đã đặt hàng thành công! Vui lòng kiểm tra email xác nhận.";
                    return RedirectToPage("/DonMuaView");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Lỗi khi đặt hàng: {error}");
                    return Page(); // Trả về trang với thông tin giỏ hàng
                }
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi hệ thống: {ex.Message}");
                return Page(); // Trả về trang với thông tin giỏ hàng
            }
        }

        // HÀM HELPER MỚI: Xây dựng email HTML
        private string BuildInvoiceEmailBody(ThanhToanInputModel customerInfo, CafebookModel.Model.ModelWeb.ThanhToanViewModel orderInfo)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"<h1>Cảm ơn bạn đã đặt hàng tại Cafebook!</h1>");
            sb.Append($"<p>Chào {customerInfo.TenNguoiNhan},</p>");
            sb.Append("<p>Chúng tôi đã nhận được đơn hàng của bạn và đang tiến hành xử lý.</p>");
            sb.Append("<hr>");
            sb.Append("<h3>Chi tiết đơn hàng</h3>");

            sb.Append("<table border='1' cellpadding='5' style='border-collapse: collapse; width: 100%;'>");
            sb.Append("<thead><tr style='background-color: #f2f2f2;'><th>Sản phẩm</th><th>Loại</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead>");
            sb.Append("<tbody>");

            foreach (var item in orderInfo.Items)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{item.TenHienThi}</td>");
                sb.Append($"<td>{(item.Loai == "Sach" ? "Sách (Cọc)" : "Sản phẩm")}</td>");
                sb.Append($"<td>{item.SoLuong}</td>");
                sb.Append($"<td>{item.DonGia:N0} đ</td>");
                sb.Append($"<td>{item.ThanhTien:N0} đ</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody><tfoot>");
            sb.Append($"<tr><td colspan='4' style='text-align: right; font-weight: bold;'>Tổng cộng</td><td style='font-weight: bold;'>{orderInfo.TongTien:N0} đ</td></tr>");
            sb.Append("</tfoot></table>");

            sb.Append("<hr>");
            sb.Append("<h3>Thông tin giao hàng</h3>");
            sb.Append($"<p><strong>Người nhận:</strong> {customerInfo.TenNguoiNhan}</p>");
            sb.Append($"<p><strong>Điện thoại:</strong> {customerInfo.SoDienThoai}</p>");
            sb.Append($"<p><strong>Địa chỉ:</strong> {customerInfo.DiaChi}</p>");
            sb.Append($"<p><strong>Thanh toán:</strong> {customerInfo.PhuongThucThanhToan}</p>");

            return sb.ToString();
        }

        private async Task<List<GioHangItemViewModel>> GetCartDetails(HttpClient httpClient, List<CartItemDto> sessionCart)
        {
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
                catch { /* Bỏ qua nếu 1 item lỗi */ }
            }
            return populatedItems;
        }
    }
}