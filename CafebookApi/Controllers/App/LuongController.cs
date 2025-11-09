// Tệp: CafebookApi/Controllers/App/LuongController.cs
// (*** THAY THẾ TOÀN BỘ TỆP NÀY ***)

using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        #region === Helpers ===
        private async Task<CaiDatNhanSuDto> LoadHrSettingsAsync()
        {
            var settings = await _context.CaiDats
                .Where(c => c.TenCaiDat.StartsWith("HR_"))
                .ToListAsync();

            T GetSettingValue<T>(string key, T defaultValue)
            {
                var setting = settings.FirstOrDefault(s => s.TenCaiDat == key)?.GiaTri;
                if (string.IsNullOrEmpty(setting)) return defaultValue;
                try
                {
                    return (T)Convert.ChangeType(setting, typeof(T), CultureInfo.InvariantCulture);
                }
                catch
                {
                    return defaultValue;
                }
            }

            return new CaiDatNhanSuDto
            {
                GioLamChuan = GetSettingValue("HR_GioLamChuan", 8.0m),
                HeSoOT = GetSettingValue("HR_HeSoOT", 1.5m),
                PhatDiTre_Phut = GetSettingValue("HR_PhatDiTre_Phut", 5),
                PhatDiTre_HeSo = GetSettingValue("HR_PhatDiTre_HeSo", 1.0m),
                ChuyenCan_SoNgay = GetSettingValue("HR_ChuyenCan_SoNgay", 26),
                ChuyenCan_TienThuong = GetSettingValue("HR_ChuyenCan_TienThuong", 500000m),
                PhepNam_MacDinh = GetSettingValue("HR_PhepNam_MacDinh", 12)
            };
        }
        #endregion

        #region Module 6.1: Bảng Chấm Công
        [HttpGet("chamcong")]
        public async Task<IActionResult> GetChamCongByDate([FromQuery] DateTime date)
        {
            var hrSettings = await LoadHrSettingsAsync();
            var targetDate = date.Date;

            var query = _context.LichLamViecs
                .Include(l => l.NhanVien)
                .Include(l => l.CaLamViec)
                .Include(l => l.BangChamCongs)
                .Where(l => l.NgayLam == targetDate);

            var results = (await query.ToListAsync())
                .Select(l =>
                {
                    var chamCong = l.BangChamCongs.FirstOrDefault();
                    var gioVao = chamCong?.GioVao;
                    var gioRa = chamCong?.GioRa;
                    var soGioLam = chamCong?.SoGioLam ?? 0m;
                    string trangThai = "Chưa chấm công";

                    if (gioVao.HasValue)
                    {
                        var gioCaVao = l.NgayLam.Add(l.CaLamViec.GioBatDau);
                        trangThai = (gioVao.Value > gioCaVao.AddMinutes(hrSettings.PhatDiTre_Phut)) ? "Đi trễ" : "Đúng giờ";
                    }

                    return new ChamCongDto
                    {
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

        [HttpPut("chamcong")]
        public async Task<IActionResult> UpdateChamCong([FromBody] ChamCongUpdateDto dto)
        {
            BangChamCong? chamCong;

            if (dto.IdChamCong == 0)
            {
                var lich = await _context.LichLamViecs.FindAsync(dto.IdLichLamViec);
                if (lich == null)
                {
                    return BadRequest("Không tìm thấy lịch làm việc tương ứng để tạo chấm công.");
                }
                chamCong = new BangChamCong
                {
                    IdLichLamViec = dto.IdLichLamViec
                };
                _context.BangChamCongs.Add(chamCong);
            }
            else
            {
                chamCong = await _context.BangChamCongs.FindAsync(dto.IdChamCong);
                if (chamCong == null) return NotFound("Không tìm thấy dữ liệu chấm công.");
            }

            chamCong.GioVao = dto.GioVaoMoi;
            chamCong.GioRa = dto.GioRaMoi;

            await _context.SaveChangesAsync();
            return Ok();
        }

        #endregion

        #region Module 6.3 & 6.4: Tính & Chốt Lương

        [HttpGet("calculate")]
        public async Task<IActionResult> CalculatePayroll([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var bangKeList = new List<LuongBangKeDto>();
            var nhanViens = await _context.NhanViens
                .Where(nv => nv.TrangThaiLamViec == "Đang làm việc")
                .ToListAsync();

            var hrSettings = await LoadHrSettingsAsync();

            decimal HE_SO_OT = hrSettings.HeSoOT;
            decimal PHAT_DI_TRE_MOI_GIO = hrSettings.PhatDiTre_HeSo;
            int SO_PHUT_TRE_CHO_PHEP = hrSettings.PhatDiTre_Phut;

            foreach (var nv in nhanViens)
            {
                var ke = new LuongBangKeDto
                {
                    IdNhanVien = nv.IdNhanVien,
                    HoTenNhanVien = nv.HoTen,
                    LuongCoBan = nv.LuongCoBan,
                    ChiTiet = ""
                };

                var lichLamViecList = await _context.LichLamViecs
                    .Include(l => l.CaLamViec)
                    .Include(l => l.BangChamCongs)
                    .Where(l => l.IdNhanVien == nv.IdNhanVien &&
                                l.NgayLam >= startDate.Date &&
                                l.NgayLam <= endDate.Date)
                    .ToListAsync();

                ke.TongGioLam = lichLamViecList.SelectMany(l => l.BangChamCongs).Sum(c => c.SoGioLam ?? 0m);
                ke.TienLuongGio = ke.TongGioLam * ke.LuongCoBan;
                ke.ChiTiet += $"Giờ: {ke.TongGioLam:F2}h. Lương: {ke.TienLuongGio:N0}. ";

                decimal tongPhatTuDong = 0;
                var diTreList = lichLamViecList
                    .Select(l => new {
                        GioCaVao = l.NgayLam.Add(l.CaLamViec.GioBatDau),
                        GioVao = l.BangChamCongs.FirstOrDefault()?.GioVao
                    })
                    .Where(x => x.GioVao.HasValue && x.GioVao.Value > x.GioCaVao.AddMinutes(SO_PHUT_TRE_CHO_PHEP))
                    .ToList();

                foreach (var tre in diTreList)
                {
                    var phutTre = (tre.GioVao!.Value - tre.GioCaVao).TotalMinutes;
                    tongPhatTuDong += (decimal)(phutTre / 60.0) * ke.LuongCoBan * PHAT_DI_TRE_MOI_GIO;
                }
                if (tongPhatTuDong > 0) ke.ChiTiet += $"Phạt trễ: {tongPhatTuDong:N0}. ";

                decimal tongThuongTuDong = 0;
                var otList = lichLamViecList
                    .Select(l => new {
                        GioCaRa = l.NgayLam.Add(l.CaLamViec.GioKetThuc),
                        GioRa = l.BangChamCongs.FirstOrDefault()?.GioRa
                    })
                    .Where(x => x.GioRa.HasValue && x.GioRa.Value > x.GioCaRa)
                    .ToList();

                foreach (var ot in otList)
                {
                    var phutOT = (ot.GioRa!.Value - ot.GioCaRa).TotalMinutes;
                    tongThuongTuDong += (decimal)(phutOT / 60.0) * ke.LuongCoBan * HE_SO_OT;
                }
                if (tongThuongTuDong > 0) ke.ChiTiet += $"Thưởng OT: {tongThuongTuDong:N0}. ";

                int soNgayLamViec = lichLamViecList
                                        .Count(l => l.BangChamCongs.Any(cc => (cc.SoGioLam ?? 0m) > 0));

                if (soNgayLamViec >= hrSettings.ChuyenCan_SoNgay)
                {
                    tongThuongTuDong += hrSettings.ChuyenCan_TienThuong;
                    ke.ChiTiet += $"Thưởng CC: {hrSettings.ChuyenCan_TienThuong:N0}. ";
                }

                // *** SỬA LỖI (Gây ra CS0103 'NhanVienIdNhanVien') ***
                // Thay vì tải toàn bộ PhieuThuongPhat, chỉ Select cột SoTien
                var thuongPhatManualTien = await _context.PhieuThuongPhats
                    .Where(p => p.IdNhanVien == nv.IdNhanVien && p.IdPhieuLuong == null)
                    .Select(p => p.SoTien) // Chỉ lấy số tiền
                    .ToListAsync();

                decimal tongThuongManual = thuongPhatManualTien.Where(soTien => soTien > 0).Sum();
                decimal tongPhatManual = thuongPhatManualTien.Where(soTien => soTien < 0).Sum() * -1; // Sum() sẽ là số âm
                // *** KẾT THÚC SỬA LỖI ***

                if (tongThuongManual > 0) ke.ChiTiet += $"Thưởng khác: {tongThuongManual:N0}. ";
                if (tongPhatManual > 0) ke.ChiTiet += $"Phạt khác: {tongPhatManual:N0}. ";

                ke.TongThuong = tongThuongTuDong + tongThuongManual;
                ke.TongPhat = tongPhatTuDong + tongPhatManual;
                ke.ThucLanh = ke.TienLuongGio + ke.TongThuong - ke.TongPhat;

                bangKeList.Add(ke);
            }

            return Ok(bangKeList);
        }


        [HttpPost("finalize")]
        public async Task<IActionResult> FinalizePayroll([FromBody] LuongFinalizeDto dto)
        {
            if (dto == null || !dto.DanhSachBangKe.Any())
                return BadRequest("Dữ liệu chốt lương không hợp lệ.");

            var existing = await _context.PhieuLuongs
                .AnyAsync(p => p.Thang == dto.Thang && p.Nam == dto.Nam);
            if (existing)
                return Conflict($"Lương Tháng {dto.Thang}/{dto.Nam} đã được chốt trước đó.");

            var newPhieuLuongs = new List<PhieuLuong>();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var ke in dto.DanhSachBangKe)
                    {
                        var phieuLuong = new PhieuLuong
                        {
                            IdNhanVien = ke.IdNhanVien,
                            Thang = dto.Thang,
                            Nam = dto.Nam,
                            LuongCoBan = ke.LuongCoBan,
                            TongGioLam = ke.TongGioLam,
                            TienThuong = ke.TongThuong,
                            KhauTru = ke.TongPhat,
                            ThucLanh = ke.ThucLanh,
                            NgayTao = DateTime.Now,
                            TrangThai = "Đã chốt"
                        };
                        newPhieuLuongs.Add(phieuLuong);
                    }
                    _context.PhieuLuongs.AddRange(newPhieuLuongs);
                    await _context.SaveChangesAsync();

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