using CafebookModel.Model.ModelWeb; // Thêm DTO
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json; // Thêm

namespace WebCafebookApi.Pages // Giả sử namespace là WebCafebookApi.Pages
{
    // Đảm bảo tên class khớp với @model
    public class TrangChuViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public TrangChuViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // KHAI BÁO CÁC THUỘC TÍNH MÀ .cshtml CẦN
        // (Sửa lỗi CS1061)
        public ThongTinChungDto? Info { get; set; }
        public List<KhuyenMaiDto> Promotions { get; set; } = new();
        public List<SanPhamDto> MonNoiBat { get; set; } = new();
        public List<SachDto> SachNoiBat { get; set; } = new();

        public async Task OnGetAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                // Gọi API Trang chủ
                var response = await httpClient.GetFromJsonAsync<TrangChuDto>("http://localhost:5166/api/web/trangchu/data");

                if (response != null)
                {
                    Info = response.Info;
                    Promotions = response.Promotions;
                    MonNoiBat = response.MonNoiBat;
                    SachNoiBat = response.SachNoiBat;
                }
            }
            catch (Exception)
            {
                // Nếu API lỗi, trang vẫn hiển thị (với dữ liệu rỗng)
                // Khởi tạo Info để tránh lỗi null ở .cshtml
                Info = new ThongTinChungDto();
            }
        }
    }
}