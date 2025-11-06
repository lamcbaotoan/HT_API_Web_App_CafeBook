using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using CafebookModel.Model.Entities; // Thêm
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq; // Cần thêm
using System.Threading.Tasks; // Cần thêm

namespace CafebookApi.Controllers.App
{
    [Route("api/app/caidat")]
    [ApiController]
    public class CaiDatController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public CaiDatController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API để lấy tất cả cài đặt
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllSettings()
        {
            var settings = await _context.CaiDats
                .Select(c => new CaiDatDto
                {
                    TenCaiDat = c.TenCaiDat,
                    GiaTri = c.GiaTri,
                    MoTa = c.MoTa
                })
                .ToListAsync();

            return Ok(settings);
        }

        /// <summary>
        /// API để cập nhật MỘT cài đặt (lưu từng cái một)
        /// </summary>
        [HttpPut("update-single")]
        public async Task<IActionResult> UpdateSingleSetting([FromBody] CaiDatDto settingToSave)
        {
            if (settingToSave == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            try
            {
                var settingInDb = await _context.CaiDats.FindAsync(settingToSave.TenCaiDat);

                if (settingInDb != null)
                {
                    settingInDb.GiaTri = settingToSave.GiaTri; // Chỉ cập nhật giá trị
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Cập nhật thành công!" });
                }

                return NotFound("Không tìm thấy cài đặt.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }

        /// <summary>
        /// API mới: Lấy thông tin cơ bản của cửa hàng
        /// </summary>
        [HttpGet("thong-tin-cua-hang")]
        public async Task<IActionResult> GetThongTinCuaHang()
        {
            var settings = await _context.CaiDats
                                    .Where(c => c.TenCaiDat == "TenQuan" ||
                                                c.TenCaiDat == "DiaChi" ||
                                                c.TenCaiDat == "SoDienThoai")
                                    .ToListAsync();

            var dto = new CaiDatThongTinCuaHangDto
            {
                TenQuan = settings.FirstOrDefault(c => c.TenCaiDat == "TenQuan")?.GiaTri ?? "Cafe Sách",
                DiaChi = settings.FirstOrDefault(c => c.TenCaiDat == "DiaChi")?.GiaTri ?? "N/A",
                SoDienThoai = settings.FirstOrDefault(c => c.TenCaiDat == "SoDienThoai")?.GiaTri ?? "N/A"
            };

            return Ok(dto);
        }
    }
}