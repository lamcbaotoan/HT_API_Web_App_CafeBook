// Tập tin: WebCafebookApi/Pages/Account/ThanhToanThanhCongView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace WebCafebookApi.Pages.Account
{
    [Authorize(Roles = "KhachHang")]
    public class ThanhToanThanhCongViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ThanhToanThanhCongViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; } // Đây là IdHoaDon

        public ThanhToanThanhCongDto? HoaDon { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id == 0)
            {
                ErrorMessage = "Không tìm thấy mã đơn hàng.";
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                // Gọi API mới để lấy tóm tắt đơn hàng
                HoaDon = await httpClient.GetFromJsonAsync<ThanhToanThanhCongDto>($"api/web/thanhtoan/order-summary/{Id}");
                if (HoaDon == null)
                {
                    ErrorMessage = "Không thể tải thông tin đơn hàng.";
                }
            }
            catch (System.Exception ex)
            {
                // Lỗi 401 (Unauthorized) hoặc 404 (NotFound) cũng sẽ bị bắt ở đây
                ErrorMessage = $"Bạn không có quyền xem đơn hàng này hoặc đơn hàng không tồn tại. ({ex.Message})";
            }

            return Page();
        }
    }
}