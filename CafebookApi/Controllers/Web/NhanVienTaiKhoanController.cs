// Tập tin: CafebookApi/Controllers/Web/NhanVienTaiKhoanController.cs
using CafebookApi.Data;
using CafebookModel.Model.Data;
using CafebookModel.Model.ModelApi;
using CafebookModel.Utils; // THÊM
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting; // THÊM
using Microsoft.Extensions.Configuration; // THÊM
using System.IO; // THÊM

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/taikhoannv")]
    [ApiController]
    public class NhanVienTaiKhoanController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        // --- THÊM MỚI (Giống các controller khác) ---
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public NhanVienTaiKhoanController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://127.0.0.1:5166";
        }

        // --- THÊM MỚI: Hàm helper ---
        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }
        // -------------------------

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.TenDangNhap) || string.IsNullOrEmpty(model.MatKhau))
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Thông tin không hợp lệ." });
            }

            var userInput = model.TenDangNhap.Trim();
            var passInput = model.MatKhau.Trim();
            // 1. TÌM KIẾM NHÂN VIÊN (Tận dụng logic từ API của WPF)
            var nhanVien = await _context.NhanViens
                .Include(nv => nv.VaiTro)
                .ThenInclude(vt => vt.VaiTroQuyens)
                .ThenInclude(vtq => vtq.Quyen)
                .FirstOrDefaultAsync(nv =>
                    (nv.TenDangNhap == userInput || nv.SoDienThoai == userInput || nv.Email == userInput) &&
                    (nv.MatKhau == passInput)
                );
            if (nhanVien == null || nhanVien.VaiTro == null)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Sai thông tin đăng nhập hoặc mật khẩu." });
            }

            // 2. TẠO NhanVienDto (SỬA ĐỔI)
            var dto = new NhanVienDto
            {
                IdNhanVien = nhanVien.IdNhanVien,
                HoTen = nhanVien.HoTen,
                // SỬA: Trả về URL thay vì Base64
                AnhDaiDien = GetFullImageUrl(nhanVien.AnhDaiDien),
                TenVaiTro = nhanVien.VaiTro.TenVaiTro,
                DanhSachQuyen = nhanVien.VaiTro.VaiTroQuyens
                                    .Select(vtq => vtq.IdQuyen)
                                    .ToList()
            };
            return Ok(new WebLoginResponseModel { Success = true, NhanVienData = dto });
        }
    }
}