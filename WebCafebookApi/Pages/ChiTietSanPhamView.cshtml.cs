// Tập tin: WebCafebookApi/Pages/ChiTietSanPhamView.cshtml.cs
using CafebookModel.Model.ModelWeb; // For SanPhamChiTietDto
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Threading.Tasks;

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

        public SanPhamChiTietDto? SanPham { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id <= 0)
            {
                ErrorMessage = "ID sản phẩm không hợp lệ.";
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                SanPham = await httpClient.GetFromJsonAsync<SanPhamChiTietDto>($"http://localhost:5166/api/web/thucdon/{Id}");
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
    }
}