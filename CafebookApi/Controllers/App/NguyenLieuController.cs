using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/nguyenlieu")]
    [ApiController]
    public class NguyenLieuController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public NguyenLieuController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API Lấy tất cả Nguyên Liệu (kèm tồn kho)
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllNguyenLieu()
        {
            var data = await _context.NguyenLieus
                .Select(nl => new NguyenLieuCrudDto
                {
                    IdNguyenLieu = nl.IdNguyenLieu,
                    TenNguyenLieu = nl.TenNguyenLieu,
                    DonViTinh = nl.DonViTinh,
                    TonKhoToiThieu = nl.TonKhoToiThieu,
                    TonKho = nl.TonKho // Lấy số lượng tồn
                })
                .OrderBy(nl => nl.TenNguyenLieu)
                .ToListAsync();
            return Ok(data);
        }

        /// <summary>
        /// API Thêm mới Nguyên Liệu
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateNguyenLieu([FromBody] NguyenLieuUpdateRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenNguyenLieu) || string.IsNullOrWhiteSpace(dto.DonViTinh))
            {
                return BadRequest("Tên và Đơn vị tính là bắt buộc.");
            }

            if (await _context.NguyenLieus.AnyAsync(nl => nl.TenNguyenLieu.ToLower() == dto.TenNguyenLieu.ToLower()))
            {
                return Conflict("Tên nguyên liệu đã tồn tại.");
            }

            var entity = new NguyenLieu
            {
                TenNguyenLieu = dto.TenNguyenLieu,
                DonViTinh = dto.DonViTinh,
                TonKhoToiThieu = dto.TonKhoToiThieu,
                TonKho = 0 // Mới tạo
            };

            _context.NguyenLieus.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        /// <summary>
        /// API Cập nhật Nguyên Liệu
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNguyenLieu(int id, [FromBody] NguyenLieuUpdateRequestDto dto)
        {
            var entity = await _context.NguyenLieus.FindAsync(id);
            if (entity == null) return NotFound();

            if (await _context.NguyenLieus.AnyAsync(nl => nl.TenNguyenLieu.ToLower() == dto.TenNguyenLieu.ToLower() && nl.IdNguyenLieu != id))
            {
                return Conflict("Tên nguyên liệu đã tồn tại.");
            }

            entity.TenNguyenLieu = dto.TenNguyenLieu;
            entity.DonViTinh = dto.DonViTinh;
            entity.TonKhoToiThieu = dto.TonKhoToiThieu;

            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// API Xóa Nguyên Liệu
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNguyenLieu(int id)
        {
            // Kiểm tra ràng buộc
            if (await _context.DinhLuongs.AnyAsync(d => d.IdNguyenLieu == id))
            {
                return Conflict("Không thể xóa. Nguyên liệu này đang được sử dụng trong Định lượng sản phẩm.");
            }
            if (await _context.ChiTietNhapKhos.AnyAsync(d => d.IdNguyenLieu == id))
            {
                return Conflict("Không thể xóa. Nguyên liệu này đã có trong lịch sử Nhập kho.");
            }

            var entity = await _context.NguyenLieus.FindAsync(id);
            if (entity == null) return NotFound();

            _context.NguyenLieus.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}