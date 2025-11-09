// Tệp: CafebookApi/Controllers/App/ThuongPhatController.cs
// (*** THAY THẾ TOÀN BỘ TỆP NÀY ***)

using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/thuongphat")]
    [ApiController]
    public class ThuongPhatController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public ThuongPhatController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API Lấy danh sách Thưởng/Phạt (chưa chốt lương)
        /// </summary>
        [HttpGet("pending/{idNhanVien}")]
        public async Task<IActionResult> GetPending(int idNhanVien)
        {
            // *** SỬA LỖI: Dùng Select thủ công thay vì Include ***
            var data = await _context.PhieuThuongPhats
                .Where(p => p.IdNhanVien == idNhanVien && p.IdPhieuLuong == null) // Chỉ lấy phiếu chưa chốt
                .OrderByDescending(p => p.NgayTao)
                .Select(p => new PhieuThuongPhatDto
                {
                    IdPhieuThuongPhat = p.IdPhieuThuongPhat,
                    IdNhanVien = p.IdNhanVien,
                    // Dùng truy vấn con (subquery)
                    HoTenNhanVien = _context.NhanViens.Where(nv => nv.IdNhanVien == p.IdNhanVien).Select(nv => nv.HoTen).FirstOrDefault() ?? "N/A",
                    NgayTao = p.NgayTao,
                    SoTien = p.SoTien,
                    LyDo = p.LyDo,
                    // Dùng truy vấn con (subquery)
                    TenNguoiTao = _context.NhanViens.Where(nv => nv.IdNhanVien == p.IdNguoiTao).Select(nv => nv.HoTen).FirstOrDefault() ?? "N/A"
                })
                .ToListAsync();
            // *** KẾT THÚC SỬA LỖI ***

            return Ok(data);
        }

        /// <summary>
        /// API Thêm mới Thưởng/Phạt
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PhieuThuongPhatCreateDto dto)
        {
            var entity = new PhieuThuongPhat
            {
                IdNhanVien = dto.IdNhanVien,
                IdNguoiTao = dto.IdNguoiTao,
                NgayTao = DateTime.Now,
                SoTien = dto.SoTien,
                LyDo = dto.LyDo,
                IdPhieuLuong = null // Chưa chốt
            };

            _context.PhieuThuongPhats.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        /// <summary>
        /// API Xóa Thưởng/Phạt (chỉ khi chưa chốt)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.PhieuThuongPhats.FindAsync(id);
            if (entity == null) return NotFound();

            if (entity.IdPhieuLuong != null)
            {
                return Conflict("Không thể xóa. Khoản này đã được chốt trong phiếu lương.");
            }

            _context.PhieuThuongPhats.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}