using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims; // Cần dùng để lấy ID

namespace WebCafebookApi.Pages.employee
{
    // Giả định bạn đã cấu hình Authorize cho thư mục /employee
    public class SoDoBanViewModel : PageModel
    {
        public int NhanVienId { get; set; }
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            var idNhanVienStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idNhanVienStr) || !int.TryParse(idNhanVienStr, out var id))
            {
                // Nếu không có cookie, đá về trang đăng nhập
                return RedirectToPage("/Employee/DangNhapEmployee");
            }

            // Gán ID nhân viên để .cshtml có thể đọc
            NhanVienId = id;
            return Page();
        }
    }
}