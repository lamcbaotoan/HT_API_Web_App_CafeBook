// Tệp: CafebookApi/Controllers/App/NhanVien/LichLamViecController.cs
using CafebookApi.Data;
using CafebookModel.Model.ModelApp.NhanVien;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App.NhanVien
{
    [Route("api/app/nhanvien/lichlamviec")]
    [ApiController]
    [Authorize]
    public class LichLamViecController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public LichLamViecController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy lịch làm việc cho 1 tuần (hoặc 1 ngày)
        /// </summary>
        [HttpGet("tuan")]
        public async Task<IActionResult> GetLichLamViecTuan([FromQuery] DateTime ngayTrongTuan)
        {
            try
            {
                int idNhanVien = GetCurrentUserId();
                if (idNhanVien == 0) return Unauthorized();

                // 1. Tính toán ngày (Thứ 2 - Chủ Nhật)
                int diff = (7 + (int)ngayTrongTuan.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                var startOfWeek = ngayTrongTuan.AddDays(-1 * diff).Date;
                var endOfWeek = startOfWeek.AddDays(6).Date;

                // 2. Truy vấn Lịch Làm Việc (Ca làm)
                var lichLamViec = await _context.LichLamViecs
                    .Include(l => l.CaLamViec)
                    .Where(l => l.IdNhanVien == idNhanVien &&
                                l.NgayLam >= startOfWeek &&
                                l.NgayLam <= endOfWeek &&
                                l.CaLamViec != null)
                    .Select(l => new LichLamViecItemDto
                    {
                        IdLichLamViec = l.IdLichLamViec,
                        TenCa = l.CaLamViec.TenCa,
                        NgayLam = l.NgayLam,
                        GioBatDau = l.CaLamViec.GioBatDau,
                        GioKetThuc = l.CaLamViec.GioKetThuc
                    })
                    .ToListAsync();

                // 3. Truy vấn Đơn Xin Nghỉ (Đã duyệt)
                var donNghi = await _context.DonXinNghis
                    .Where(d => d.IdNhanVien == idNhanVien &&
                                d.TrangThai == "Đã duyệt" &&
                                d.NgayBatDau <= endOfWeek &&
                                d.NgayKetThuc >= startOfWeek)
                    .Select(d => new LichNghiItemDto
                    {
                        LoaiDon = d.LoaiDon,
                        NgayBatDau = d.NgayBatDau,
                        NgayKetThuc = d.NgayKetThuc
                    })
                    .ToListAsync();

                // 4. Tạo DTO trả về
                var responseDto = new LichLamViecViewDto
                {
                    NgayBatDauTuan = startOfWeek,
                    NgayKetThucTuan = endOfWeek,
                    LichLamViecTrongTuan = lichLamViec,
                    DonNghiTrongTuan = donNghi
                };

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }

        /// <summary>
        /// THÊM MỚI: Lấy lịch làm việc cho 1 tháng
        /// </summary>
        [HttpGet("thang")]
        public async Task<IActionResult> GetLichLamViecThang([FromQuery] DateTime ngayTrongThang)
        {
            try
            {
                int idNhanVien = GetCurrentUserId();
                if (idNhanVien == 0) return Unauthorized();

                var startOfMonth = new DateTime(ngayTrongThang.Year, ngayTrongThang.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                // 1. Lấy ca làm
                var caLam = await _context.LichLamViecs
                    .Include(l => l.CaLamViec)
                    .Where(l => l.IdNhanVien == idNhanVien &&
                                l.NgayLam >= startOfMonth &&
                                l.NgayLam <= endOfMonth &&
                                l.CaLamViec != null)
                    .Select(l => new
                    {
                        l.NgayLam,
                        SuKien = $"{l.CaLamViec.TenCa} ({l.CaLamViec.GioBatDau:hh\\:mm}-{l.CaLamViec.GioKetThuc:hh\\:mm})"
                    })
                    .ToListAsync();

                // 2. Lấy đơn nghỉ
                var donNghi = await _context.DonXinNghis
                    .Where(d => d.IdNhanVien == idNhanVien &&
                                d.TrangThai == "Đã duyệt" &&
                                d.NgayBatDau <= endOfMonth &&
                                d.NgayKetThuc >= startOfMonth)
                    .ToListAsync();

                // 3. Tổng hợp 2 danh sách
                var allEvents = new List<dynamic>();
                allEvents.AddRange(caLam);

                // Mở rộng đơn nghỉ thành các ngày riêng lẻ
                foreach (var nghi in donNghi)
                {
                    for (DateTime day = nghi.NgayBatDau.Date; day <= nghi.NgayKetThuc.Date; day = day.AddDays(1))
                    {
                        if (day >= startOfMonth && day <= endOfMonth)
                        {
                            allEvents.Add(new { NgayLam = day, SuKien = nghi.LoaiDon });
                        }
                    }
                }

                // 4. Nhóm theo ngày
                var groupedEvents = allEvents
                    .GroupBy(e => e.NgayLam)
                    .Select(g => new LichLamViecNgayDto
                    {
                        Ngay = g.Key,
                        SuKien = g.Select(ev => (string)ev.SuKien).ToList()
                    })
                    .ToList();

                return Ok(new LichLamViecThangDto { NgayCoSuKien = groupedEvents });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }


        // === HÀM HELPER ===
        private int GetCurrentUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "IdNhanVien");
            if (idClaim != null && int.TryParse(idClaim.Value, out int idNhanVien))
            {
                return idNhanVien;
            }
            return 0;
        }
    }
}