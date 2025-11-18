using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/nguoigiaohang")]
    [ApiController]
    public class NguoiGiaoHangController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public NguoiGiaoHangController(CafebookDbContext context)
        {
            _context = context;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.NguoiGiaoHangs
                .Select(n => new NguoiGiaoHangCrudDto
                {
                    IdNguoiGiaoHang = n.IdNguoiGiaoHang,
                    TenNguoiGiaoHang = n.TenNguoiGiaoHang,
                    SoDienThoai = n.SoDienThoai,
                    TrangThai = n.TrangThai
                })
                .OrderBy(n => n.TenNguoiGiaoHang)
                .ToListAsync();
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NguoiGiaoHangCrudDto dto)
        {
            if (await _context.NguoiGiaoHangs.AnyAsync(n => n.TenNguoiGiaoHang.ToLower() == dto.TenNguoiGiaoHang.ToLower()))
            {
                return Conflict("Tên đơn vị vận chuyển đã tồn tại.");
            }

            var entity = new NguoiGiaoHang
            {
                TenNguoiGiaoHang = dto.TenNguoiGiaoHang,
                SoDienThoai = dto.SoDienThoai,
                TrangThai = dto.TrangThai
            };
            _context.NguoiGiaoHangs.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] NguoiGiaoHangCrudDto dto)
        {
            var entity = await _context.NguoiGiaoHangs.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TenNguoiGiaoHang = dto.TenNguoiGiaoHang;
            entity.SoDienThoai = dto.SoDienThoai;
            entity.TrangThai = dto.TrangThai;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Không cho xóa nếu đã có đơn hàng gắn với shipper này
            if (await _context.HoaDons.AnyAsync(h => h.IdNguoiGiaoHang == id))
            {
                return Conflict("Không thể xóa. Đơn vị này đã có lịch sử giao hàng.");
            }

            var entity = await _context.NguoiGiaoHangs.FindAsync(id);
            if (entity == null) return NotFound();

            _context.NguoiGiaoHangs.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}