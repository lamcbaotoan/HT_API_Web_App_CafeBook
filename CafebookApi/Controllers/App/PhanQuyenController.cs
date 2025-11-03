using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/phanquyen")]
    [ApiController]
    public class PhanQuyenController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public PhanQuyenController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API Lấy TẤT CẢ các Quyền có trong hệ thống
        /// </summary>
        [HttpGet("all-permissions")]
        public async Task<IActionResult> GetAllPermissions()
        {
            var data = await _context.Quyens
                .AsNoTracking()
                .Select(q => new QuyenDto
                {
                    IdQuyen = q.IdQuyen,
                    TenQuyen = q.TenQuyen,
                    NhomQuyen = q.NhomQuyen
                })
                .OrderBy(q => q.NhomQuyen)
                .ThenBy(q => q.TenQuyen)
                .ToListAsync();
            return Ok(data);
        }

        /// <summary>
        /// API Lấy danh sách IdQuyen đã được gán cho một VaiTrò
        /// </summary>
        [HttpGet("for-role/{idVaiTro}")]
        public async Task<IActionResult> GetPermissionsForRole(int idVaiTro)
        {
            var data = await _context.VaiTroQuyens
                .Where(vtq => vtq.IdVaiTro == idVaiTro)
                .Select(vtq => vtq.IdQuyen)
                .ToListAsync();
            return Ok(data);
        }

        /// <summary>
        /// API Cập nhật (Sync) toàn bộ quyền cho một Vai Trò
        /// </summary>
        [HttpPut("update")]
        public async Task<IActionResult> UpdatePermissions([FromBody] PhanQuyenDto dto)
        {
            if (dto == null) return BadRequest("Dữ liệu không hợp lệ.");

            // 1. Lấy các quyền hiện tại
            var currentPermissions = await _context.VaiTroQuyens
                .Where(vtq => vtq.IdVaiTro == dto.IdVaiTro)
                .ToListAsync();

            var currentIdList = currentPermissions.Select(p => p.IdQuyen).ToList();
            var newIdList = dto.DanhSachIdQuyen;

            // 2. Tìm các quyền cần XÓA
            var toDelete = currentPermissions
                .Where(p => !newIdList.Contains(p.IdQuyen))
                .ToList();

            // 3. Tìm các quyền cần THÊM
            var toAddIds = newIdList
                .Where(id => !currentIdList.Contains(id))
                .ToList();

            // 4. Thực thi
            if (toDelete.Any())
            {
                _context.VaiTroQuyens.RemoveRange(toDelete);
            }

            if (toAddIds.Any())
            {
                var toAddEntities = toAddIds.Select(id => new VaiTro_Quyen
                {
                    IdVaiTro = dto.IdVaiTro,
                    IdQuyen = id
                });
                await _context.VaiTroQuyens.AddRangeAsync(toAddEntities);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật phân quyền thành công!" });
        }
    }
}