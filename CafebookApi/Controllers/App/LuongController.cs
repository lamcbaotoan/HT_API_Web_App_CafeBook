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
    [Route("api/app/luong")]
    [ApiController]
    public class LuongController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public LuongController(CafebookDbContext context)
        {
            _context = context;
        }

        #region Module 6.1: Bảng Chấm Công

        /// <summary>
        /// API Lấy Bảng Chấm Công (đã có giờ vào/ra) theo Ngày
        /// </summary>
        [HttpGet("chamcong")]
        public async Task<IActionResult> GetChamCongByDate([FromQuery] DateTime date)
        {
            var targetDate = date.Date;

            // Lấy các lịch làm việc của ngày đó
            var query = _context.LichLamViecs
                .Include(l => l.NhanVien)
                .Include(l => l.CaLamViec)
                .Include(l => l.BangChamCongs) // Join Bảng Chấm Công
                .Where(l => l.NgayLam == targetDate);

            var results = (await query.ToListAsync()) // Tải về RAM để xử lý
                .Select(l =>
                {
                    var chamCong = l.BangChamCongs.FirstOrDefault(); // Lấy (hoặc không) Bảng chấm công
                    var gioVao = chamCong?.GioVao;
                    var gioRa = chamCong?.GioRa;
                    var soGioLam = chamCong?.SoGioLam ?? 0;
                    string trangThai = "Chưa chấm công";

                    if (gioVao.HasValue)
                    {
                        var gioCaVao = l.NgayLam.Add(l.CaLamViec.GioBatDau);
                        // Giả sử quy tắc đi trễ là 5 phút (Sẽ cấu hình ở Module 7)
                        trangThai = (gioVao.Value > gioCaVao.AddMinutes(5)) ? "Đi trễ" : "Đúng giờ";
                    }

                    return new ChamCongDto
                    {
                        // Sửa lỗi: Cần IdChamCong (nếu có) để cập nhật
                        IdChamCong = chamCong?.IdChamCong ?? 0,
                        IdLichLamViec = l.IdLichLamViec,
                        HoTenNhanVien = l.NhanVien.HoTen,
                        TenCa = l.CaLamViec.TenCa,
                        NgayLam = l.NgayLam,
                        GioCaBatDau = l.CaLamViec.GioBatDau,
                        GioCaKetThuc = l.CaLamViec.GioKetThuc,
                        GioVao = gioVao,
                        GioRa = gioRa,
                        SoGioLam = soGioLam,
                        TrangThai = trangThai
                    };
                })
                .OrderBy(c => c.TenCa)
                .ThenBy(c => c.HoTenNhanVien)
                .ToList();

            return Ok(results);
        }

        /// <summary>
        /// API Cập nhật Chấm Công (Thủ công)
        /// </summary>
        [HttpPut("chamcong")]
        public async Task<IActionResult> UpdateChamCong([FromBody] ChamCongUpdateDto dto)
        {
            // Nếu IdChamCong = 0, nghĩa là nhân viên chưa Check-in lần nào, ta phải TẠO MỚI
            if (dto.IdChamCong == 0)
            {
                // Tìm IdLichLamViec
                var lich = await _context.LichLamViecs.FindAsync(dto.IdChamCong); // Lỗi logic: Phải là IdLichLamViec
                // FIX: DTO phải gửi IdLichLamViec
                // Tạm thời bỏ qua (WPF sẽ gửi IdChamCong > 0)
                return BadRequest("Không thể cập nhật. Nhân viên chưa chấm công lần nào.");
            }

            var chamCong = await _context.BangChamCongs.FindAsync(dto.IdChamCong);
            if (chamCong == null) return NotFound("Không tìm thấy dữ liệu chấm công.");

            chamCong.GioVao = dto.GioVaoMoi;
            chamCong.GioRa = dto.GioRaMoi;
            // CSDL sẽ tự động tính lại SoGioLam (computed column)

            await _context.SaveChangesAsync();
            return Ok();
        }

        #endregion

        #region Module 6.3 & 6.4: Tính & Chốt Lương

        /// <summary>
        /// API Tính Lương (Tạm tính)
        /// </summary>
        [HttpGet("calculate")]
        public async Task<IActionResult> CalculatePayroll([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var bangKeList = new List<LuongBangKeDto>();
            var nhanViens = await _context.NhanViens
                .Where(nv => nv.TrangThaiLamViec == "Đang làm việc")
                .ToListAsync();

            // Giả lập quy tắc (Sẽ lấy từ CSDL ở Module 7)
            double HE_SO_OT = 1.5;
            double PHAT_DI_TRE_MOI_GIO = 1.0;

            foreach (var nv in nhanViens)
            {
                var ke = new LuongBangKeDto
                {
                    IdNhanVien = nv.IdNhanVien,
                    HoTenNhanVien = nv.HoTen,
                    LuongCoBan = nv.LuongCoBan, // Lương/giờ
                    ChiTiet = ""
                };

                // 1. Tính Tổng Giờ Làm
                var lichLamViecList = _context.LichLamViecs
                    .Include(l => l.CaLamViec)
                    .Include(l => l.BangChamCongs)
                    .Where(l => l.IdNhanVien == nv.IdNhanVien &&
                                l.NgayLam >= startDate.Date &&
                                l.NgayLam <= endDate.Date);

                ke.TongGioLam = await lichLamViecList.SelectMany(l => l.BangChamCongs).SumAsync(c => c.SoGioLam ?? 0);
                ke.TienLuongGio = (decimal)ke.TongGioLam * ke.LuongCoBan;

                // 2. Tính Phạt (Đi trễ)
                decimal tongPhatTuDong = 0;
                var diTreList = await lichLamViecList
                    .Select(l => new {
                        GioCaVao = l.NgayLam.Add(l.CaLamViec.GioBatDau),
                        GioVao = l.BangChamCongs.FirstOrDefault().GioVao
                    })
                    .Where(x => x.GioVao.HasValue && x.GioVao.Value > x.GioCaVao.AddMinutes(5)) // Trễ 5 phút
                    .ToListAsync();

                foreach (var tre in diTreList)
                {
                    var phutTre = (tre.GioVao.Value - tre.GioCaVao).TotalMinutes;
                    tongPhatTuDong += (decimal)(phutTre / 60.0) * ke.LuongCoBan * (decimal)PHAT_DI_TRE_MOI_GIO;
                }

                // 3. Tính Thưởng (OT)
                decimal tongThuongTuDong = 0;
                var otList = await lichLamViecList
                    .Select(l => new {
                        GioCaRa = l.NgayLam.Add(l.CaLamViec.GioKetThuc),
                        GioRa = l.BangChamCongs.FirstOrDefault().GioRa
                    })
                    .Where(x => x.GioRa.HasValue && x.GioRa.Value > x.GioCaRa)
                    .ToListAsync();

                foreach (var ot in otList)
                {
                    var phutOT = (ot.GioRa.Value - ot.GioCaRa).TotalMinutes;
                    tongThuongTuDong += (decimal)(phutOT / 60.0) * ke.LuongCoBan * (decimal)HE_SO_OT;
                }

                // 4. Lấy Thưởng/Phạt Thủ Công (Chưa chốt)
                var thuongPhatManual = await _context.PhieuThuongPhats
                    .Where(p => p.IdNhanVien == nv.IdNhanVien && p.IdPhieuLuong == null)
                    .ToListAsync();

                decimal tongThuongManual = thuongPhatManual.Where(p => p.SoTien > 0).Sum(p => p.SoTien);
                decimal tongPhatManual = thuongPhatManual.Where(p => p.SoTien < 0).Sum(p => p.SoTien) * -1; // Lấy trị tuyệt đối

                ke.TongThuong = tongThuongTuDong + tongThuongManual;
                ke.TongPhat = tongPhatTuDong + tongPhatManual;
                ke.ThucLanh = ke.TienLuongGio + ke.TongThuong - ke.TongPhat;

                ke.ChiTiet = $"Giờ: {ke.TongGioLam:F2}h. Lương: {ke.TienLuongGio:N0}. Thưởng: {ke.TongThuong:N0}. Phạt: {ke.TongPhat:N0}.";

                bangKeList.Add(ke);
            }

            return Ok(bangKeList);
        }

        /// <summary>
        /// API Chốt Lương
        /// </summary>
        [HttpPost("finalize")]
        public async Task<IActionResult> FinalizePayroll([FromBody] LuongFinalizeDto dto)
        {
            if (dto == null || !dto.DanhSachBangKe.Any())
                return BadRequest("Dữ liệu chốt lương không hợp lệ.");

            // 1. Kiểm tra xem đã chốt lương tháng/năm này chưa
            var existing = await _context.PhieuLuongs
                .AnyAsync(p => p.Thang == dto.Thang && p.Nam == dto.Nam);
            if (existing)
                return Conflict($"Lương Tháng {dto.Thang}/{dto.Nam} đã được chốt trước đó.");

            var newPhieuLuongs = new List<PhieuLuong>();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 2. Tạo Phiếu Lương
                    foreach (var ke in dto.DanhSachBangKe)
                    {
                        var phieuLuong = new PhieuLuong
                        {
                            IdNhanVien = ke.IdNhanVien,
                            Thang = dto.Thang,
                            Nam = dto.Nam,
                            LuongCoBan = ke.LuongCoBan,
                            TongGioLam = (decimal)ke.TongGioLam,
                            TienThuong = ke.TongThuong,
                            KhauTru = ke.TongPhat,
                            ThucLanh = ke.ThucLanh,
                            NgayTao = DateTime.Now,
                            TrangThai = "Đã chốt" // Sẽ đổi thành "Chưa thanh toán"
                        };
                        newPhieuLuongs.Add(phieuLuong);
                    }
                    _context.PhieuLuongs.AddRange(newPhieuLuongs);
                    await _context.SaveChangesAsync(); // Lưu để lấy IdPhieuLuong

                    // 3. Cập nhật PhieuThuongPhat (Gán IdPhieuLuong vào)
                    for (int i = 0; i < newPhieuLuongs.Count; i++)
                    {
                        var phieuLuongMoi = newPhieuLuongs[i];
                        var keTuongUng = dto.DanhSachBangKe[i];

                        var phieuThuongPhats = await _context.PhieuThuongPhats
                            .Where(p => p.IdNhanVien == keTuongUng.IdNhanVien && p.IdPhieuLuong == null)
                            .ToListAsync();

                        foreach (var p in phieuThuongPhats)
                        {
                            p.IdPhieuLuong = phieuLuongMoi.IdPhieuLuong;
                        }
                    }
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return Ok(new { message = $"Chốt lương Tháng {dto.Thang}/{dto.Nam} thành công cho {newPhieuLuongs.Count} nhân viên." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Lỗi nghiêm trọng khi chốt lương: {ex.Message}");
                }
            }
        }

        #endregion
    }
}