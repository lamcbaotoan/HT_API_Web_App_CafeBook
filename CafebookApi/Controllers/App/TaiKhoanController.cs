using CafebookApi.Data;
using CafebookModel.Model.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq; // Thêm
using System.Threading.Tasks; // Thêm

namespace CafebookApi.Controllers.App
{
    [Route("api/app/taikhoan")]
    [ApiController]
    public class TaiKhoanController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public TaiKhoanController(CafebookDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.TenDangNhap) || string.IsNullOrEmpty(model.MatKhau))
            {
                return BadRequest(new LoginResponseModel { Success = false, Message = "Tên đăng nhập và mật khẩu không được rỗng." });
            }

            // ---------- BẮT ĐẦU SỬA ĐỔI ----------

            // 1. Lấy thông tin đăng nhập và trim()
            // Biến "userInput" có thể là TenDangNhap, Email, hoặc SoDienThoai
            var userInput = model.TenDangNhap.Trim();
            var passInput = model.MatKhau.Trim();

            // 2. Sửa câu lệnh truy vấn để kiểm tra 3 cột
            var nhanVien = await _context.NhanViens
                .Include(nv => nv.VaiTro)
                    .ThenInclude(vt => vt.VaiTroQuyens)
                    .ThenInclude(vtq => vtq.Quyen)
                .FirstOrDefaultAsync(nv =>
                    // Kiểm tra 1 trong 3 cột này
                    (nv.TenDangNhap == userInput || nv.SoDienThoai == userInput || nv.Email == userInput) &&
                    // VÀ mật khẩu phải khớp
                    (nv.MatKhau == passInput)
                );

            // ---------- KẾT THÚC SỬA ĐỔI ----------

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
                AnhDaiDien = nhanVien.AnhDaiDien, // Chuỗi Base64
                TenVaiTro = nhanVien.VaiTro.TenVaiTro,
                DanhSachQuyen = nhanVien.VaiTro.VaiTroQuyens
                                    .Select(vtq => vtq.IdQuyen) // Sửa: Lấy IdQuyen
                                    .ToList()
            };

            // TODO: Tạo JWT Token ở đây nếu cần
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