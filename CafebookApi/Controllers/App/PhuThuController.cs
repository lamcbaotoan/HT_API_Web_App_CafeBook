using CafebookApi.Data;
using CafebookModel.Model.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App.QuanLy
{
    [Route("api/app/quanly/phuthu")]
    [ApiController]
    public class PhuThuController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public PhuThuController(CafebookDbContext context)
        {
            _context = context;
        }

        // GET: api/app/quanly/phuthu
        [HttpGet]
        public async Task<IActionResult> GetAllPhuThu()
        {
            var phuThus = await _context.PhuThus.AsNoTracking().ToListAsync();
            return Ok(phuThus);
        }

        // GET: api/app/quanly/phuthu/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPhuThuById(int id)
        {
            var phuThu = await _context.PhuThus.FindAsync(id);
            if (phuThu == null)
            {
                return NotFound();
            }
            return Ok(phuThu);
        }

        // POST: api/app/quanly/phuthu
        [HttpPost]
        public async Task<IActionResult> CreatePhuThu([FromBody] PhuThu phuThu)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.PhuThus.Add(phuThu);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPhuThuById), new { id = phuThu.IdPhuThu }, phuThu);
        }

        // PUT: api/app/quanly/phuthu/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePhuThu(int id, [FromBody] PhuThu phuThu)
        {
            if (id != phuThu.IdPhuThu)
            {
                return BadRequest("ID không khớp.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(phuThu).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.PhuThus.Any(e => e.IdPhuThu == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return NoContent();
        }

        // DELETE: api/app/quanly/phuthu/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhuThu(int id)
        {
            var phuThu = await _context.PhuThus.FindAsync(id);
            if (phuThu == null)
            {
                return NotFound();
            }

            // KIỂM TRA QUAN TRỌNG: Không cho xóa nếu phụ thu đã được dùng
            var isInUse = await _context.ChiTietPhuThuHoaDons.AnyAsync(ct => ct.IdPhuThu == id);
            if (isInUse)
            {
                return Conflict("Không thể xóa phụ thu này vì đã được sử dụng trong các hóa đơn cũ.");
            }

            _context.PhuThus.Remove(phuThu);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}