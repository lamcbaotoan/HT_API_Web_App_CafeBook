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
    [Route("api/app/calamviec")]
    [ApiController]
    public class CaLamViecController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public CaLamViecController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API Lấy tất cả Ca Làm Việc (Mẫu)
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.CaLamViecs
                .Select(c => new CaLamViecDto
                {
                    IdCa = c.IdCa,
                    TenCa = c.TenCa,
                    GioBatDau = c.GioBatDau,
                    GioKetThuc = c.GioKetThuc
                })
                .OrderBy(c => c.GioBatDau)
                .ToListAsync();
            return Ok(data);
        }

        /// <summary>
        /// API Thêm mới Ca Mẫu
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CaLamViecDto dto)
        {
            var entity = new CaLamViec
            {
                TenCa = dto.TenCa,
                GioBatDau = dto.GioBatDau,
                GioKetThuc = dto.GioKetThuc
            };
            _context.CaLamViecs.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        /// <summary>
        /// API Cập nhật Ca Mẫu
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CaLamViecDto dto)
        {
            var entity = await _context.CaLamViecs.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TenCa = dto.TenCa;
            entity.GioBatDau = dto.GioBatDau;
            entity.GioKetThuc = dto.GioKetThuc;

            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// API Xóa Ca Mẫu
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Kiểm tra ràng buộc
            if (await _context.LichLamViecs.AnyAsync(l => l.IdCa == id))
            {
                return Conflict("Không thể xóa. Ca làm việc này đang được sử dụng trong Lịch Làm Việc.");
            }

            var entity = await _context.CaLamViecs.FindAsync(id);
            if (entity == null) return NotFound();

            _context.CaLamViecs.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}