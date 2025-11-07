using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp.NhanVien.DatBan;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppCafebookApi.Controllers.app.NhanVien
{
    [Route("api/app/datban")]
    [ApiController]
    public class DatBanController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IConfiguration _config;

        public DatBanController(CafebookDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpGet("list")]
        public async Task<ActionResult<IEnumerable<PhieuDatBanDto>>> GetDatBanList()
        {
            var list = await _context.PhieuDatBans
                .Include(p => p.KhachHang)
                .Include(p => p.Ban).ThenInclude(b => b.KhuVuc)
                .OrderByDescending(p => p.ThoiGianDat)
                .Select(p => new PhieuDatBanDto
                {
                    IdPhieuDatBan = p.IdPhieuDatBan,
                    // SỬA: Thêm kiểm tra null
                    TenKhachHang = p.KhachHang != null ? p.KhachHang.HoTen : p.HoTenKhach,
                    SoDienThoai = p.KhachHang != null ? p.KhachHang.SoDienThoai : p.SdtKhach,
                    Email = p.KhachHang != null ? p.KhachHang.Email : null,
                    IdBan = p.IdBan,
                    SoBan = p.Ban.SoBan,
                    TenKhuVuc = p.Ban.KhuVuc != null ? p.Ban.KhuVuc.TenKhuVuc : "N/A",
                    ThoiGianDat = p.ThoiGianDat,
                    SoLuongKhach = p.SoLuongKhach,
                    TrangThai = p.TrangThai,
                    GhiChu = p.GhiChu,
                    IdKhachHang = p.IdKhachHang
                }).ToListAsync();

            return Ok(list);
        }

        [HttpGet("available-bans")]
        public async Task<ActionResult<IEnumerable<BanDatBanDto>>> GetAvailableBans()
        {
            var bans = await _context.Bans
                .Include(b => b.KhuVuc)
                .Where(b => b.TrangThai == "Trống" || b.TrangThai == "Đã đặt")
                .Select(b => new BanDatBanDto
                {
                    IdBan = b.IdBan,
                    SoBan = b.SoBan,
                    // SỬA: Thêm kiểm tra null
                    TenKhuVuc = b.KhuVuc != null ? b.KhuVuc.TenKhuVuc : "N/A",
                    SoGhe = b.SoGhe,
                    IdKhuVuc = b.IdKhuVuc
                }).ToListAsync();
            return Ok(bans);
        }

        [HttpGet("search-customer")]
        public async Task<ActionResult<IEnumerable<KhachHangLookupDto>>> SearchCustomer([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 3)
            {
                return Ok(new List<KhachHangLookupDto>());
            }

            var results = await _context.KhachHangs
                .Where(kh => (kh.SoDienThoai != null && kh.SoDienThoai.Contains(query)) ||
                             (kh.Email != null && kh.Email.Contains(query)) ||
                             kh.HoTen.Contains(query))
                .Select(kh => new KhachHangLookupDto
                {
                    IdKhachHang = kh.IdKhachHang,
                    HoTen = kh.HoTen,
                    SoDienThoai = kh.SoDienThoai ?? "", // Sửa: Gán default
                    Email = kh.Email
                })
                .Take(10)
                .ToListAsync();

            return Ok(results);
        }


        [HttpPost("create-staff")]
        public async Task<IActionResult> CreateDatBanStaff(PhieuDatBanCreateUpdateDto dto)
        {
            var khachHang = await FindOrCreateKhachHang(dto.TenKhachHang, dto.SoDienThoai, dto.Email);
            var ban = await _context.Bans.FindAsync(dto.IdBan);
            if (ban == null) return BadRequest("Bàn không tồn tại.");
            if (ban.TrangThai == "Có khách") return BadRequest("Bàn đang có khách, không thể đặt.");

            if (dto.SoLuongKhach > ban.SoGhe)
            {
                return BadRequest($"Số lượng khách ({dto.SoLuongKhach}) vượt quá số ghế của bàn ({ban.SoGhe}).");
            }

            DateTime requestedTime = dto.ThoiGianDat;
            DateTime timeStart = requestedTime.AddHours(-2);
            DateTime timeEnd = requestedTime.AddHours(2);

            bool isConflict = await _context.PhieuDatBans.AnyAsync(p =>
                p.IdBan == dto.IdBan &&
                (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận") &&
                (p.ThoiGianDat >= timeStart && p.ThoiGianDat <= timeEnd)
            );
            if (isConflict)
            {
                return Conflict("Bàn này đã được đặt trong khung giờ này (xung đột +/- 2 tiếng).");
            }

            var phieu = new PhieuDatBan
            {
                IdKhachHang = khachHang.IdKhachHang,
                IdBan = dto.IdBan,
                HoTenKhach = khachHang.HoTen,
                SdtKhach = khachHang.SoDienThoai,
                ThoiGianDat = dto.ThoiGianDat,
                SoLuongKhach = dto.SoLuongKhach,
                GhiChu = dto.GhiChu,
                TrangThai = "Đã xác nhận"
            };
            _context.PhieuDatBans.Add(phieu);
            ban.TrangThai = "Đã đặt";
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(khachHang.Email))
            {
                await SendConfirmationEmailAsync(phieu, khachHang, ban.SoBan);
            }
            return Ok(new { idPhieuDatBan = phieu.IdPhieuDatBan });
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateDatBan(int id, PhieuDatBanCreateUpdateDto dto)
        {
            var phieu = await _context.PhieuDatBans
                .Include(p => p.KhachHang)
                .Include(p => p.Ban)
                .FirstOrDefaultAsync(p => p.IdPhieuDatBan == id);

            if (phieu == null) return NotFound("Không tìm thấy phiếu đặt.");

            string oldTrangThai = phieu.TrangThai;

            var khachHang = await FindOrCreateKhachHang(dto.TenKhachHang, dto.SoDienThoai, dto.Email);
            phieu.IdKhachHang = khachHang.IdKhachHang;
            phieu.HoTenKhach = khachHang.HoTen;
            phieu.SdtKhach = khachHang.SoDienThoai;

            if (phieu.IdBan != dto.IdBan)
            {
                var oldBan = await _context.Bans.FindAsync(phieu.IdBan);
                if (oldBan != null) oldBan.TrangThai = "Trống";
                var newBan = await _context.Bans.FindAsync(dto.IdBan);
                if (newBan == null) return BadRequest("Bàn mới không tồn tại.");
                if (newBan.TrangThai == "Có khách") return BadRequest("Bàn mới đang có khách.");

                if (dto.SoLuongKhach > newBan.SoGhe)
                {
                    return BadRequest($"Số lượng khách ({dto.SoLuongKhach}) vượt quá số ghế của bàn mới ({newBan.SoGhe}).");
                }

                newBan.TrangThai = "Đã đặt";
                phieu.IdBan = dto.IdBan;
            }

            phieu.ThoiGianDat = dto.ThoiGianDat;
            phieu.SoLuongKhach = dto.SoLuongKhach;
            phieu.GhiChu = dto.GhiChu;
            phieu.TrangThai = dto.TrangThai;

            await _context.SaveChangesAsync();

            if (oldTrangThai == "Chờ xác nhận" && dto.TrangThai == "Đã xác nhận")
            {
                // SỬA: Thêm kiểm tra null
                var khachHangThucTe = phieu.KhachHang ?? khachHang;
                if (khachHangThucTe != null && !string.IsNullOrEmpty(khachHangThucTe.Email))
                {
                    await SendConfirmationEmailAsync(phieu, khachHangThucTe, phieu.Ban.SoBan);
                }
            }

            return Ok();
        }

        [HttpPost("xacnhan-den")]
        public async Task<ActionResult<XacNhanKhachDenResponseDto>> XacNhanKhachDen(XacNhanKhachDenRequestDto dto)
        {
            var phieu = await _context.PhieuDatBans
                .Include(p => p.Ban)
                .FirstOrDefaultAsync(p => p.IdPhieuDatBan == dto.IdPhieuDatBan);

            if (phieu == null) return NotFound("Phiếu đặt không tồn tại.");
            // SỬA: Thêm kiểm tra null
            if (phieu.Ban == null) return BadRequest("Bàn không hợp lệ.");

            var ban = phieu.Ban;
            if (ban.TrangThai == "Có khách")
            {
                return BadRequest($"Bàn {ban.SoBan} đã có khách. Vui lòng kiểm tra lại.");
            }

            var hoaDon = new HoaDon
            {
                IdBan = phieu.IdBan,
                IdNhanVien = dto.IdNhanVien,
                IdKhachHang = phieu.IdKhachHang,
                ThoiGianTao = DateTime.Now,
                TrangThai = "Chưa thanh toán",
                LoaiHoaDon = "Tại quán",
                TongTienGoc = 0,
                GiamGia = 0,
                TongPhuThu = 0
            };
            _context.HoaDons.Add(hoaDon);
            await _context.SaveChangesAsync();

            phieu.TrangThai = "Khách đã đến";
            ban.TrangThai = "Có khách";

            await _context.SaveChangesAsync();
            return Ok(new XacNhanKhachDenResponseDto { IdHoaDon = hoaDon.IdHoaDon });
        }

        [HttpPost("huy/{id}")]
        public async Task<IActionResult> HuyDatBan(int id)
        {
            var phieu = await _context.PhieuDatBans.Include(p => p.Ban)
                        .FirstOrDefaultAsync(p => p.IdPhieuDatBan == id);
            if (phieu == null) return NotFound("Phiếu đặt không tồn tại.");

            if (phieu.TrangThai == "Đã hủy" || phieu.TrangThai == "Khách đã đến")
            {
                return BadRequest("Không thể hủy phiếu ở trạng thái này.");
            }
            phieu.TrangThai = "Đã hủy";

            if (phieu.Ban != null)
            {
                phieu.Ban.TrangThai = "Trống";
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("create-web")]
        public async Task<IActionResult> CreateDatBanWeb(PhieuDatBanWebCreateDto dto)
        {
            var khachHang = await FindOrCreateKhachHang(dto.TenKhachHang, dto.SoDienThoai, dto.Email);
            var ban = await _context.Bans.FindAsync(dto.IdBan);
            if (ban == null) return BadRequest("Bàn không tồn tại.");
            if (ban.TrangThai != "Trống") return BadRequest("Bàn đã có người đặt hoặc đang có khách.");

            if (dto.SoLuongKhach > ban.SoGhe)
            {
                return BadRequest($"Số lượng khách ({dto.SoLuongKhach}) vượt quá số ghế của bàn ({ban.SoGhe}).");
            }

            DateTime requestedTime = dto.ThoiGianDat;
            DateTime timeStart = requestedTime.AddHours(-2);
            DateTime timeEnd = requestedTime.AddHours(2);
            bool isConflict = await _context.PhieuDatBans.AnyAsync(p =>
                p.IdBan == dto.IdBan &&
                (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận") &&
                (p.ThoiGianDat >= timeStart && p.ThoiGianDat <= timeEnd)
            );
            if (isConflict)
            {
                return Conflict("Bàn này đã được đặt trong khung giờ này (xung đột +/- 2 tiếng).");
            }

            var phieu = new PhieuDatBan
            {
                IdKhachHang = khachHang.IdKhachHang,
                IdBan = dto.IdBan,
                HoTenKhach = khachHang.HoTen,
                SdtKhach = khachHang.SoDienThoai,
                ThoiGianDat = dto.ThoiGianDat,
                SoLuongKhach = dto.SoLuongKhach,
                GhiChu = dto.GhiChu,
                TrangThai = "Chờ xác nhận"
            };
            _context.PhieuDatBans.Add(phieu);
            ban.TrangThai = "Đã đặt";
            await _context.SaveChangesAsync();

            var noiDungTB = $"Phiếu đặt bàn #{phieu.IdPhieuDatBan} ({khachHang.HoTen}) đang chờ xác nhận.";

            var thongBao = new ThongBao
            {
                IdNhanVienTao = null,
                NoiDung = noiDungTB,
                LoaiThongBao = "DatBan",
                IdLienQuan = phieu.IdPhieuDatBan,
                DaXem = false,
                ThoiGianTao = DateTime.Now
            };
            _context.ThongBaos.Add(thongBao);
            await _context.SaveChangesAsync();

            return Ok(new { idPhieuDatBan = phieu.IdPhieuDatBan });
        }


        // === Helpers ===
        private async Task<KhachHang> FindOrCreateKhachHang(string ten, string sdt, string? email)
        {
            // SỬA: Gán giá trị mặc định cho email nếu nó null
            string emailToSearch = email ?? string.Empty;

            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.SoDienThoai == sdt || (!string.IsNullOrEmpty(emailToSearch) && k.Email == emailToSearch));

            if (khachHang == null)
            {
                khachHang = new KhachHang
                {
                    HoTen = ten,
                    SoDienThoai = sdt,
                    Email = email,
                    DiemTichLuy = 0,
                    NgayTao = DateTime.Now,
                    BiKhoa = false
                };
                _context.KhachHangs.Add(khachHang);
                await _context.SaveChangesAsync();
            }
            else
            {
                khachHang.HoTen = ten;
                khachHang.Email = string.IsNullOrEmpty(email) ? khachHang.Email : email;
                await _context.SaveChangesAsync();
            }
            return khachHang;
        }

        private async Task SendConfirmationEmailAsync(PhieuDatBan phieu, KhachHang khach, string soBan)
        {
            // SỬA: Thêm kiểm tra null
            if (khach.Email == null)
            {
                Console.WriteLine("Bỏ qua gửi email: Email khách hàng là null.");
                return;
            }

            try
            {
                var smtpSettings = _config.GetSection("SmtpSettings");
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(smtpSettings["FromName"], smtpSettings["Username"]));
                email.To.Add(new MailboxAddress(khach.HoTen, khach.Email));
                email.Subject = "[Cafebook] Xác nhận đặt bàn thành công";

                string body = $@"
                    <p>Xin chào {khach.HoTen},</p>
                    <p>Cảm ơn bạn đã đặt bàn tại Cafebook.</p>
                    <p><strong>Thông tin đặt bàn:</strong></p>
                    <ul>
                        <li><strong>Bàn:</strong> {soBan}</li>
                        <li><strong>Thời gian:</strong> {phieu.ThoiGianDat:HH:mm} ngày {phieu.ThoiGianDat:dd/MM/yyyy}</li>
                        <li><strong>Số khách:</strong> {phieu.SoLuongKhach}</li>
                        <li><strong>Ghi chú:</strong> {phieu.GhiChu ?? "Không có"}</li>
                        <li><strong>Trạng thái:</strong> {phieu.TrangThai}</li>
                    </ul>
                    <p>Rất mong được đón tiếp bạn!</p>
                    <p>Trân trọng,<br>Đội ngũ Cafebook</p>";

                email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(smtpSettings["Host"], int.Parse(smtpSettings["Port"] ?? "0"), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpSettings["Username"], smtpSettings["Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email: {ex.Message}");
            }
        }
    }
}