using CafebookApi.Data;
using CafebookModel.Model.Data;
using CafebookModel.Model.ModelApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/taikhoannv")]
    [ApiController]
    public class NhanVienTaiKhoanController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public NhanVienTaiKhoanController(CafebookDbContext context)
        {
            _context = context;
        }

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

            // 2. TẠO NhanVienDto (Tận dụng DTO của WPF)
            var dto = new NhanVienDto
            {
                IdNhanVien = nhanVien.IdNhanVien,
                HoTen = nhanVien.HoTen,
                AnhDaiDien = nhanVien.AnhDaiDien, // Base64
                TenVaiTro = nhanVien.VaiTro.TenVaiTro,
                DanhSachQuyen = nhanVien.VaiTro.VaiTroQuyens
                                    .Select(vtq => vtq.IdQuyen)
                                    .ToList()
            };

            return Ok(new WebLoginResponseModel { Success = true, NhanVienData = dto });
        }
    }
}