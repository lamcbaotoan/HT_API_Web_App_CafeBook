// Tập tin: CafebookApi/Controllers/App/TaiKhoanController.cs
using CafebookApi.Data;
using CafebookModel.Model.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApi;
using CafebookModel.Utils; // <-- THÊM
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting; // <-- THÊM
using Microsoft.Extensions.Configuration; // <-- THÊM
using System.IO; // <-- THÊM

namespace CafebookApi.Controllers.App
{
    [Route("api/app/taikhoan")]
    [ApiController]
    public class TaiKhoanController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        // --- SỬA: THÊM CÁC BIẾN ĐỂ XỬ LÝ URL ---
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public TaiKhoanController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config) // <-- SỬA: Thêm tham số
        {
            _context = context;

            // --- THÊM LOGIC KHỞI TẠO TỪ SANPHAMCONTROLLER ---
            _env = env;
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if (!Directory.Exists(_env.WebRootPath))
                {
                    Directory.CreateDirectory(_env.WebRootPath);
                }
            }
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url")
                             ?? "http://127.0.0.1:5166"; // <-- Dùng 127.0.0.1
        }

        // --- THÊM HÀM HELPER GetFullImageUrl ---
        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            // Đảm bảo đường dẫn dùng "/"
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel model)
        {
            // ... (Logic kiểm tra model, truy vấn CSDL giữ nguyên) ...
            var userInput = model.TenDangNhap.Trim();
            var passInput = model.MatKhau.Trim();

            var nhanVien = await _context.NhanViens
              .Include(nv => nv.VaiTro)
                .ThenInclude(vt => vt.VaiTroQuyens)
                .ThenInclude(vtq => vtq.Quyen)
              .FirstOrDefaultAsync(nv =>
                (nv.TenDangNhap == userInput || nv.SoDienThoai == userInput || nv.Email == userInput) &&
                (nv.MatKhau == passInput)
              );

            if (nhanVien == null)
            {
                return Ok(new LoginResponseModel { Success = false, Message = "Sai tên đăng nhập hoặc mật khẩu." });
            }

            if (nhanVien.VaiTro == null)
            {
                return Ok(new LoginResponseModel { Success = false, Message = "Tài khoản không có vai trò." });
            }

            // Tạo DTO để trả về
            var userDto = new NhanVienDto
            {
                IdNhanVien = nhanVien.IdNhanVien,
                HoTen = nhanVien.HoTen,

                // SỬA: Dùng GetFullImageUrl
                AnhDaiDien = GetFullImageUrl(nhanVien.AnhDaiDien), // Chuyển path thành URL

                TenVaiTro = nhanVien.VaiTro.TenVaiTro,
                DanhSachQuyen = nhanVien.VaiTro.VaiTroQuyens
                  .Select(vtq => vtq.IdQuyen)
                  .ToList()
            };

            string token = "day_la_jwt_token_tam_thoi";

            return Ok(new LoginResponseModel
            {
                Success = true,
                Message = "Đăng nhập thành công!",
                Token = token,
                UserData = userDto
            });
        }
    }
}