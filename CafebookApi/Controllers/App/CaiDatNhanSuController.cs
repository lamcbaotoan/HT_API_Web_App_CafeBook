using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/caidatnhansu")]
    [ApiController]
    public class CaiDatNhanSuController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public CaiDatNhanSuController(CafebookDbContext context)
        {
            _context = context;
        }

        // Hàm helper để đọc Cài đặt an toàn
        private string GetSetting(List<CaiDat> settings, string key, string defaultValue)
        {
            return settings.FirstOrDefault(c => c.TenCaiDat == key)?.GiaTri ?? defaultValue;
        }

        /// <summary>
        /// API Lấy tất cả Cài đặt Nhân sự
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllHrSettings()
        {
            var allSettings = await _context.CaiDats
                .Where(c => c.TenCaiDat.StartsWith("HR_"))
                .ToListAsync();

            // Dùng CultureInfo.InvariantCulture để đảm bảo dấu chấm (.) là dấu thập phân
            var dto = new CaiDatNhanSuDto
            {
                GioLamChuan = double.Parse(GetSetting(allSettings, "HR_GioLamChuan", "8"), CultureInfo.InvariantCulture),
                HeSoOT = double.Parse(GetSetting(allSettings, "HR_HeSoOT", "1.5"), CultureInfo.InvariantCulture),
                PhatDiTre_Phut = int.Parse(GetSetting(allSettings, "HR_PhatDiTre_Phut", "5")),
                PhatDiTre_HeSo = double.Parse(GetSetting(allSettings, "HR_PhatDiTre_HeSo", "1.0"), CultureInfo.InvariantCulture),
                ChuyenCan_SoNgay = int.Parse(GetSetting(allSettings, "HR_ChuyenCan_SoNgay", "26")),
                ChuyenCan_TienThuong = decimal.Parse(GetSetting(allSettings, "HR_ChuyenCan_TienThuong", "0"), CultureInfo.InvariantCulture),
                PhepNam_MacDinh = int.Parse(GetSetting(allSettings, "HR_PhepNam_MacDinh", "12"))
            };

            return Ok(dto);
        }

        /// <summary>
        /// API Cập nhật (Lưu) tất cả Cài đặt Nhân sự
        /// </summary>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateHrSettings([FromBody] CaiDatNhanSuDto dto)
        {
            if (dto == null) return BadRequest("Dữ liệu không hợp lệ.");

            // Dùng Transaction để đảm bảo lưu tất cả hoặc không lưu gì cả
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Hàm helper để cập nhật Cài đặt
                    async Task UpdateSetting(string key, string value)
                    {
                        var setting = await _context.CaiDats.FindAsync(key);
                        if (setting != null)
                        {
                            setting.GiaTri = value;
                        }
                        // Nếu không tìm thấy (do SQL script chưa chạy), thì tạo mới
                        else
                        {
                            _context.CaiDats.Add(new CaiDat { TenCaiDat = key, GiaTri = value, MoTa = "Auto-generated HR Setting" });
                        }
                    }

                    // Dùng CultureInfo.InvariantCulture để lưu dấu chấm (.)
                    await UpdateSetting("HR_GioLamChuan", dto.GioLamChuan.ToString(CultureInfo.InvariantCulture));
                    await UpdateSetting("HR_HeSoOT", dto.HeSoOT.ToString(CultureInfo.InvariantCulture));
                    await UpdateSetting("HR_PhatDiTre_Phut", dto.PhatDiTre_Phut.ToString());
                    await UpdateSetting("HR_PhatDiTre_HeSo", dto.PhatDiTre_HeSo.ToString(CultureInfo.InvariantCulture));
                    await UpdateSetting("HR_ChuyenCan_SoNgay", dto.ChuyenCan_SoNgay.ToString());
                    await UpdateSetting("HR_ChuyenCan_TienThuong", dto.ChuyenCan_TienThuong.ToString(CultureInfo.InvariantCulture));
                    await UpdateSetting("HR_PhepNam_MacDinh", dto.PhepNam_MacDinh.ToString());

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { message = "Lưu cài đặt nhân sự thành công!" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Lỗi khi lưu cài đặt: {ex.Message}");
                }
            }
        }
    }
}