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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient; // Cần thêm để bắt lỗi SQL

namespace AppCafebookApi.Controllers.app.NhanVien
{
    [Route("api/app/datban")]
    [ApiController]
    public class DatBanController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IConfiguration _config;
        private const int ReservationSlotHours = 2; // Giả định một suất đặt bàn kéo dài 2 tiếng
        private const int ReservationBufferMinutes = 5; // 5 phút đệm

        private class OpeningHours
        {
            public TimeSpan Open { get; set; } = new TimeSpan(6, 0, 0);
            public TimeSpan Close { get; set; } = new TimeSpan(23, 0, 0);
            public bool IsValid { get; set; } = false;
        }

        public DatBanController(CafebookDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        #region GET Endpoints
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
                    SoDienThoai = kh.SoDienThoai ?? "",
                    Email = kh.Email
                })
                .Take(10).ToListAsync();
            return Ok(results);
        }

        [HttpGet("opening-hours")]
        public async Task<ActionResult<string>> GetOpeningHours()
        {
            var setting = await _context.CaiDats
                .FirstOrDefaultAsync(cd => cd.TenCaiDat == "LienHe_GioMoCua");
            if (setting == null || string.IsNullOrEmpty(setting.GiaTri))
            {
                return Ok("06:00 - 23:00");
            }
            var match = Regex.Match(setting.GiaTri, @"(\d{2}:\d{2})\s*-\s*(\d{2}:\d{2})");
            if (match.Success)
            {
                return Ok(match.Value);
            }
            return Ok("06:00 - 23:00");
        }
        #endregion

        #region POST/PUT Endpoints (Logic chính)

        [HttpPost("create-staff")]
        public async Task<IActionResult> CreateDatBanStaff(PhieuDatBanCreateUpdateDto dto)
        {
            KhachHang khachHang = null;

            // SỬA ĐỔI: Chỉ tìm/tạo KH nếu không phải là khách vãng lai
            if (!dto.IsKhachVangLai)
            {
                try
                {
                    khachHang = await FindOrCreateKhachHang(dto.TenKhachHang, dto.SoDienThoai, dto.Email);
                }
                catch (Exception ex)
                {
                    // Trả về lỗi nếu FindOrCreateKhachHang thất bại (ví dụ: Email trùng)
                    return BadRequest(ex.Message);
                }
            }
            // Nếu IsKhachVangLai == true, thì khachHang sẽ là null

            var ban = await _context.Bans.FindAsync(dto.IdBan);
            if (ban == null) return BadRequest("Bàn không tồn tại.");
            if (ban.TrangThai == "Có khách") return BadRequest("Bàn đang có khách, không thể đặt.");

            var openingHours = await GetAndParseOpeningHours();
            if (!IsTimeValid(dto.ThoiGianDat, openingHours))
            {
                return BadRequest($"Giờ đặt ({dto.ThoiGianDat:HH:mm}) nằm ngoài giờ mở cửa ({openingHours.Open:hh\\:mm} - {openingHours.Close:hh\\:mm}).");
            }
            if (dto.SoLuongKhach > ban.SoGhe)
            {
                return BadRequest($"Số lượng khách ({dto.SoLuongKhach}) vượt quá số ghế của bàn ({ban.SoGhe}).");
            }

            // Logic kiểm tra xung đột (chồng chéo slot + 5 phút đệm)
            DateTime newSlotStart = dto.ThoiGianDat;
            DateTime newSlotEnd = dto.ThoiGianDat.AddHours(ReservationSlotHours);

            var conflict = await _context.PhieuDatBans
                .Where(p => p.IdBan == dto.IdBan &&
                            (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận"))
                .ToListAsync();

            foreach (var p in conflict)
            {
                DateTime existingSlotStart = p.ThoiGianDat.AddMinutes(-ReservationBufferMinutes);
                DateTime existingSlotEnd = p.ThoiGianDat.AddHours(ReservationSlotHours).AddMinutes(ReservationBufferMinutes);

                if (newSlotStart < existingSlotEnd && existingSlotStart < newSlotEnd)
                {
                    return Conflict($"Xung đột! Bàn này đã có phiếu đặt lúc {p.ThoiGianDat:HH:mm} (có 5 phút đệm).");
                }
            }

            var phieu = new PhieuDatBan
            {
                IdKhachHang = khachHang?.IdKhachHang, // Sẽ là null nếu là khách vãng lai
                IdBan = dto.IdBan,
                HoTenKhach = dto.TenKhachHang, // Luôn lưu tên/SĐT từ form
                SdtKhach = dto.SoDienThoai,
                ThoiGianDat = dto.ThoiGianDat,
                SoLuongKhach = dto.SoLuongKhach,
                GhiChu = dto.GhiChu,
                TrangThai = dto.TrangThai
            };
            _context.PhieuDatBans.Add(phieu);
            ban.TrangThai = "Đã đặt";

            // Chỉ SaveChanges 1 lần ở đây (cho Phiếu, Bàn, và Khách (nếu có))
            await _context.SaveChangesAsync();

            // Gửi email
            var emailNguoiNhan = khachHang?.Email ?? dto.Email;
            if (!string.IsNullOrEmpty(emailNguoiNhan))
            {
                var khachInfo = new KhachHang { HoTen = dto.TenKhachHang, Email = emailNguoiNhan };
                _ = SendConfirmationEmailAsync(phieu, khachInfo, ban.SoBan);
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

            var openingHours = await GetAndParseOpeningHours();
            if (!IsTimeValid(dto.ThoiGianDat, openingHours))
            {
                return BadRequest($"Giờ đặt ({dto.ThoiGianDat:HH:mm}) nằm ngoài giờ mở cửa ({openingHours.Open:hh\\:mm} - {openingHours.Close:hh\\:mm}).");
            }

            string oldTrangThai = phieu.TrangThai;

            KhachHang khachHang = null;
            // SỬA ĐỔI: Logic tương tự Create
            if (!dto.IsKhachVangLai)
            {
                try
                {
                    khachHang = await FindOrCreateKhachHang(dto.TenKhachHang, dto.SoDienThoai, dto.Email);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            phieu.IdKhachHang = khachHang?.IdKhachHang;
            phieu.HoTenKhach = dto.TenKhachHang;
            phieu.SdtKhach = dto.SoDienThoai;

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

            // Kiểm tra xung đột khi Sửa
            DateTime newSlotStart = dto.ThoiGianDat;
            DateTime newSlotEnd = dto.ThoiGianDat.AddHours(ReservationSlotHours);
            var conflict = await _context.PhieuDatBans
                .Where(p => p.IdBan == dto.IdBan &&
                            p.IdPhieuDatBan != id && // Loại trừ chính nó
                            (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận"))
                .ToListAsync();

            foreach (var p in conflict)
            {
                DateTime existingSlotStart = p.ThoiGianDat.AddMinutes(-ReservationBufferMinutes);
                DateTime existingSlotEnd = p.ThoiGianDat.AddHours(ReservationSlotHours).AddMinutes(ReservationBufferMinutes);
                if (newSlotStart < existingSlotEnd && existingSlotStart < newSlotEnd)
                {
                    return Conflict($"Xung đột! Bàn này đã có phiếu đặt lúc {p.ThoiGianDat:HH:mm} (có 5 phút đệm).");
                }
            }

            phieu.ThoiGianDat = dto.ThoiGianDat;
            phieu.SoLuongKhach = dto.SoLuongKhach;
            phieu.GhiChu = dto.GhiChu;
            phieu.TrangThai = dto.TrangThai;

            // Lưu tất cả thay đổi
            await _context.SaveChangesAsync();

            // Gửi email
            if (oldTrangThai == "Chờ xác nhận" && dto.TrangThai == "Đã xác nhận")
            {
                var emailNguoiNhan = khachHang?.Email ?? dto.Email;
                if (!string.IsNullOrEmpty(emailNguoiNhan) && phieu.Ban != null)
                {
                    var khachInfo = new KhachHang { HoTen = dto.TenKhachHang, Email = emailNguoiNhan };
                    _ = SendConfirmationEmailAsync(phieu, khachInfo, phieu.Ban.SoBan);
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
            // Người dùng web luôn được coi là khách hàng thật (không vãng lai)
            KhachHang khachHang = null;
            try
            {
                khachHang = await FindOrCreateKhachHang(dto.TenKhachHang, dto.SoDienThoai, dto.Email);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var ban = await _context.Bans.FindAsync(dto.IdBan);
            if (ban == null) return BadRequest("Bàn không tồn tại.");
            if (ban.TrangThai != "Trống") return BadRequest("Bàn đã có người đặt hoặc đang có khách.");

            var openingHours = await GetAndParseOpeningHours();
            if (!IsTimeValid(dto.ThoiGianDat, openingHours))
            {
                return BadRequest($"Giờ đặt ({dto.ThoiGianDat:HH:mm}) nằm ngoài giờ mở cửa ({openingHours.Open:hh\\:mm} - {openingHours.Close:hh\\:mm}).");
            }
            if (dto.SoLuongKhach > ban.SoGhe)
            {
                return BadRequest($"Số lượng khách ({dto.SoLuongKhach}) vượt quá số ghế của bàn ({ban.SoGhe}).");
            }

            DateTime newSlotStart = dto.ThoiGianDat;
            DateTime newSlotEnd = dto.ThoiGianDat.AddHours(ReservationSlotHours);
            var conflict = await _context.PhieuDatBans
                .Where(p => p.IdBan == dto.IdBan &&
                            (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận"))
                .ToListAsync();
            foreach (var p in conflict)
            {
                DateTime existingSlotStart = p.ThoiGianDat.AddMinutes(-ReservationBufferMinutes);
                DateTime existingSlotEnd = p.ThoiGianDat.AddHours(ReservationSlotHours).AddMinutes(ReservationBufferMinutes);
                if (newSlotStart < existingSlotEnd && existingSlotStart < newSlotEnd)
                {
                    return Conflict($"Xung đột! Bàn này đã có phiếu đặt lúc {p.ThoiGianDat:HH:mm} (có 5 phút đệm).");
                }
            }

            var phieu = new PhieuDatBan
            {
                IdKhachHang = khachHang?.IdKhachHang, // Gán IdKhachHang
                IdBan = dto.IdBan,
                HoTenKhach = dto.TenKhachHang,
                SdtKhach = dto.SoDienThoai,
                ThoiGianDat = dto.ThoiGianDat,
                SoLuongKhach = dto.SoLuongKhach,
                GhiChu = dto.GhiChu,
                TrangThai = "Chờ xác nhận"
            };
            _context.PhieuDatBans.Add(phieu);
            ban.TrangThai = "Đã đặt";

            // Lưu lần 1 (Phiếu, Bàn, Khách hàng (nếu có))
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

            // Lưu lần 2 (Thông báo)
            await _context.SaveChangesAsync();
            return Ok(new { idPhieuDatBan = phieu.IdPhieuDatBan });
        }
        #endregion

        #region Helpers (Khách hàng, Email, Giờ)

        // SỬA ĐỔI: Thêm lại hàm FindOrCreateKhachHang
        private async Task<KhachHang> FindOrCreateKhachHang(string ten, string sdt, string? email)
        {
            // SĐT là bắt buộc để tìm/tạo
            if (string.IsNullOrWhiteSpace(sdt) || sdt == "N/A")
            {
                // Không thể tìm/tạo khách mà không có SĐT
                return null;
            }

            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.SoDienThoai == sdt);

            if (khachHang == null)
            {
                // Không tìm thấy SĐT -> Tạo mới
                khachHang = new KhachHang
                {
                    HoTen = ten,
                    SoDienThoai = sdt,
                    Email = email, // email có thể là null
                    DiemTichLuy = 0,
                    NgayTao = DateTime.Now,
                    BiKhoa = false
                };
                _context.KhachHangs.Add(khachHang);
                try
                {
                    // SaveChanges được gọi ở đây (hoặc trong hàm cha)
                    // Chúng ta sẽ để hàm cha (Create/Update) gọi SaveChanges
                }
                catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
                {
                    // Bắt lỗi UNIQUE KEY (ví dụ: Email NULL hoặc Email bị trùng)
                    // Lỗi này xảy ra do thiết kế CSDL của bạn.
                    // Cách xử lý tốt nhất là thay đổi CSDL để cho phép nhiều Email NULL:
                    // ví dụ: CREATE UNIQUE INDEX UQ_Email ON KhachHang(Email) WHERE Email IS NOT NULL;

                    // Ném lỗi để hàm cha bắt
                    throw new Exception($"Lỗi CSDL: Không thể tạo khách hàng. SĐT hoặc Email có thể đã tồn tại, hoặc CSDL đang chặn Email rỗng. (Mã lỗi: {sqlEx.Number})");
                }
            }
            else
            {
                // Tìm thấy -> Cập nhật tên và email
                khachHang.HoTen = ten;
                khachHang.Email = string.IsNullOrEmpty(email) ? khachHang.Email : email;
                // Hàm cha sẽ gọi SaveChanges
            }
            return khachHang;
        }

        private async Task SendConfirmationEmailAsync(PhieuDatBan phieu, KhachHang khach, string soBan)
        {
            if (khach.Email == null)
            {
                Console.WriteLine("Bỏ qua gửi email: Email khách hàng là null.");
                return;
            }
            try
            {
                var smtpSettings = _config.GetSection("SmtpSettings");
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(smtpSettings["FromName"] ?? "Notify", smtpSettings["Username"]));
                email.To.Add(new MailboxAddress(khach.HoTen, khach.Email));
                email.Subject = "[Cafebook] Xác nhận đặt bàn thành công";
                string body = $@"<p>Xin chào {khach.HoTen},</p>
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
                Console.WriteLine($"Lỗi gửi email (chạy ngầm): {ex.Message}");
            }
        }

        private async Task<OpeningHours> GetAndParseOpeningHours()
        {
            var setting = await _context.CaiDats
                .FirstOrDefaultAsync(cd => cd.TenCaiDat == "LienHe_GioMoCua");
            string settingValue = (setting != null && !string.IsNullOrEmpty(setting.GiaTri)) ? setting.GiaTri : "06:00 - 23:00";
            return ParseOpeningHours(settingValue);
        }

        private OpeningHours ParseOpeningHours(string settingValue)
        {
            var hours = new OpeningHours();
            try
            {
                var match = Regex.Match(settingValue, @"(\d{2}:\d{2})\s*-\s*(\d{2}:\d{2})");
                if (match.Success)
                {
                    if (TimeSpan.TryParse(match.Groups[1].Value, out TimeSpan open)) hours.Open = open;
                    if (TimeSpan.TryParse(match.Groups[2].Value, out TimeSpan close)) hours.Close = close;
                    hours.IsValid = true;
                }
            }
            catch { }
            return hours;
        }

        private bool IsTimeValid(DateTime thoiGianDat, OpeningHours hours)
        {
            var timeOfDay = thoiGianDat.TimeOfDay;
            return timeOfDay >= hours.Open && timeOfDay <= hours.Close;
        }
        #endregion
    }
}