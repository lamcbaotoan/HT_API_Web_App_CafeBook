using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp.NhanVien;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration; // THÊM MỚI
using System.Net.Mail; // THÊM MỚI
using System.Net; // THÊM MỚI

namespace CafebookApi.Controllers.App.NhanVien
{
    [Route("api/app/nhanvien/thuesach")]
    [ApiController]
    public class ThueSachController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IConfiguration _configuration; // SỬA LỖI CS0103

        public ThueSachController(CafebookDbContext context, IConfiguration configuration) // SỬA LỖI CS0103
        {
            _context = context;
            _configuration = configuration; // SỬA LỖI CS0103
        }

        // Tải các cài đặt chung
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _context.CaiDats
                .Where(c => c.TenCaiDat.StartsWith("Sach_") || c.TenCaiDat.StartsWith("DiemTichLuy_"))
                .ToListAsync();

            var dto = new CaiDatThueSachDto
            {
                PhiThue = decimal.Parse(settings.FirstOrDefault(c => c.TenCaiDat == "Sach_PhiThue")?.GiaTri ?? "5000"),
                PhiTraTreMoiNgay = decimal.Parse(settings.FirstOrDefault(c => c.TenCaiDat == "Sach_PhiTraTreMoiNgay")?.GiaTri ?? "2000"),
                SoNgayMuonToiDa = int.Parse(settings.FirstOrDefault(c => c.TenCaiDat == "Sach_SoNgayMuonToiDa")?.GiaTri ?? "7"),
                DiemPhieuThue = int.Parse(settings.FirstOrDefault(c => c.TenCaiDat == "Sach_DiemPhieuThue")?.GiaTri ?? "1"),
                PointToVND = decimal.Parse(settings.FirstOrDefault(c => c.TenCaiDat == "DiemTichLuy_DoiVND")?.GiaTri ?? "1000")
            };
            return Ok(dto);
        }

        /// <summary>
        /// CẢI TIẾN (F3): Cải tiến tìm kiếm phiếu
        /// </summary>
        [HttpGet("phieuthue")]
        public async Task<IActionResult> GetPhieuThue([FromQuery] string? search, [FromQuery] string status = "Đang Thuê")
        {
            var query = _context.PhieuThueSachs.AsQueryable();
            if (status == "Đang Thuê" || status == "Đã Trả") { query = query.Where(p => p.TrangThai == status); }
            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                int.TryParse(search, out int phieuId);
                query = query.Where(p => p.IdPhieuThueSach == phieuId || p.KhachHang.HoTen.ToLower().Contains(searchLower) || (p.KhachHang.SoDienThoai != null && p.KhachHang.SoDienThoai.Contains(search)));
            }
            var phieus = await query.Include(p => p.KhachHang).Include(p => p.ChiTietPhieuThues).OrderByDescending(p => p.NgayThue).ToListAsync();
            var dtos = phieus.Select(p => new PhieuThueGridDto
            {
                IdPhieuThueSach = p.IdPhieuThueSach,
                HoTenKH = p.KhachHang.HoTen,
                SoDienThoaiKH = p.KhachHang.SoDienThoai,
                NgayThue = p.NgayThue,
                NgayHenTra = p.ChiTietPhieuThues.Where(ct => ct.NgayTraThucTe == null).Select(ct => (DateTime?)ct.NgayHenTra).Min() ?? p.NgayThue,
                SoLuongSach = p.ChiTietPhieuThues.Count(ct => ct.NgayTraThucTe == null),
                TongTienCoc = p.TongTienCoc,
                TrangThai = p.TrangThai,
                TinhTrang = (p.TrangThai == "Đã Trả") ? "Hoàn tất" : (p.ChiTietPhieuThues.Any(ct => ct.NgayTraThucTe == null && ct.NgayHenTra < DateTime.Today) ? "Trễ Hạn" : "Đúng Hạn")
            }).ToList();
            return Ok(dtos);
        }

        // Lấy chi tiết 1 phiếu
        [HttpGet("chitiet/{idPhieu}")]
        public async Task<IActionResult> GetChiTietPhieu(int idPhieu)
        {
            var phieu = await _context.PhieuThueSachs.Include(p => p.KhachHang).Include(p => p.ChiTietPhieuThues).ThenInclude(ct => ct.Sach).FirstOrDefaultAsync(p => p.IdPhieuThueSach == idPhieu);
            if (phieu == null) return NotFound();
            var settings = await GetSettingsInternal();
            var now = DateTime.Now;
            var dto = new PhieuThueChiTietDto
            {
                IdPhieuThueSach = phieu.IdPhieuThueSach,
                HoTenKH = phieu.KhachHang.HoTen,
                SoDienThoaiKH = phieu.KhachHang.SoDienThoai,
                EmailKH = phieu.KhachHang.Email,
                DiemTichLuyKH = phieu.KhachHang.DiemTichLuy,
                NgayThue = phieu.NgayThue,
                TrangThaiPhieu = phieu.TrangThai,
                SachDaThue = phieu.ChiTietPhieuThues.Select(ct => {
                    bool treHan = ct.NgayTraThucTe == null && ct.NgayHenTra < now.Date;
                    int daysLate = treHan ? (int)(now.Date - ct.NgayHenTra).TotalDays : 0;
                    return new ChiTietSachThueDto
                    {
                        IdPhieuThueSach = ct.IdPhieuThueSach,
                        IdSach = ct.IdSach,
                        TenSach = ct.Sach.TenSach,
                        NgayHenTra = ct.NgayHenTra,
                        TienCoc = ct.TienCoc,
                        TienPhat = daysLate * settings.PhiTraTreMoiNgay,
                        TinhTrang = ct.NgayTraThucTe != null ? "Đã Trả" : (treHan ? $"Trễ {daysLate} ngày" : "Đang Thuê")
                    };
                }).ToList()
            };
            return Ok(dto);
        }

        /// <summary>
        /// SỬA LỖI (dynamic): Dùng DTO an toàn
        /// </summary>
        [HttpGet("search-khachhang")]
        public async Task<IActionResult> SearchKhachHang([FromQuery] string query)
        {
            var queryLower = query.ToLower();
            var khachHangs = await _context.KhachHangs
                .Where(kh => kh.HoTen.ToLower().Contains(queryLower) || (kh.SoDienThoai != null && kh.SoDienThoai.Contains(query)))
                .Take(10)
                .Select(kh => new KhachHangSearchDto
                {
                    IdKhachHang = kh.IdKhachHang,
                    HoTen = kh.HoTen,
                    SoDienThoai = kh.SoDienThoai,
                    DiemTichLuy = kh.DiemTichLuy,
                    Email = kh.Email // SỬA: Thêm Email
                })
                .ToListAsync();
            return Ok(khachHangs);
        }

        /// <summary>
        /// CẢI TIẾN (F1): Tìm theo ID Sách
        /// </summary>
        [HttpGet("search-sach")]
        public async Task<IActionResult> SearchSach([FromQuery] string query)
        {
            var queryLower = query.ToLower();
            int.TryParse(query, out int sachId);

            var sachs = await _context.Sachs
                .Where(s => s.SoLuongHienCo > 0 &&
                            (s.TenSach.ToLower().Contains(queryLower) || s.IdSach == sachId))
                .Include(s => s.SachTacGias).ThenInclude(stg => stg.TacGia)
                .Take(10)
                .Select(s => new SachTimKiemDto
                {
                    IdSach = s.IdSach,
                    TenSach = s.TenSach,
                    TacGia = string.Join(", ", s.SachTacGias.Select(stg => stg.TacGia.TenTacGia)),
                    SoLuongHienCo = s.SoLuongHienCo,
                    GiaBia = s.GiaBia ?? 0
                })
                .ToListAsync();
            return Ok(sachs);
        }

        /// <summary>
        /// SỬA LỖI (CSDL) & CẢI TIẾN LOGIC (Tự động Tìm/Tạo Khách)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePhieuThue([FromBody] PhieuThueRequestDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var settings = await GetSettingsInternal();
                int khachHangId;

                // 1. XỬ LÝ KHÁCH HÀNG (Logic "Tìm hoặc Tạo")
                if (dto.KhachHangInfo == null || string.IsNullOrWhiteSpace(dto.KhachHangInfo.HoTen))
                {
                    return BadRequest("Tên khách hàng là bắt buộc.");
                }
                KhachHang? khach = null;
                string? sdt = dto.KhachHangInfo.SoDienThoai;
                string? email = dto.KhachHangInfo.Email;
                if (!string.IsNullOrWhiteSpace(sdt))
                {
                    khach = await _context.KhachHangs.FirstOrDefaultAsync(k => k.SoDienThoai == sdt);
                }
                if (khach == null && !string.IsNullOrWhiteSpace(email))
                {
                    khach = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Email == email);
                }
                if (khach == null)
                {
                    if (!string.IsNullOrWhiteSpace(sdt) && await _context.KhachHangs.AnyAsync(k => k.SoDienThoai == sdt || k.TenDangNhap == sdt))
                    {
                        return Conflict("Số điện thoại này đã tồn tại.");
                    }
                    if (!string.IsNullOrWhiteSpace(email) && await _context.KhachHangs.AnyAsync(k => k.Email == email))
                    {
                        return Conflict("Email này đã tồn tại.");
                    }
                    string tenDangNhap;
                    if (!string.IsNullOrWhiteSpace(sdt)) { tenDangNhap = sdt; }
                    else if (!string.IsNullOrWhiteSpace(email)) { tenDangNhap = email; }
                    else { tenDangNhap = $"temp_{Guid.NewGuid().ToString("N")[..12]}"; }

                    var newKH = new KhachHang
                    {
                        HoTen = dto.KhachHangInfo.HoTen,
                        SoDienThoai = sdt,
                        Email = email,
                        NgayTao = DateTime.Now,
                        DiemTichLuy = 0,
                        BiKhoa = false,
                        TenDangNhap = tenDangNhap,
                        MatKhau = "123456",
                        TaiKhoanTam = true
                    };
                    _context.KhachHangs.Add(newKH);
                    await _context.SaveChangesAsync();
                    khachHangId = newKH.IdKhachHang;
                }
                else
                {
                    khachHangId = khach.IdKhachHang;
                }

                // 2. Tạo Phiếu Thuê
                decimal tongCoc = dto.SachCanThue.Sum(s => s.TienCoc);
                var phieuThue = new PhieuThueSach
                {
                    IdKhachHang = khachHangId,
                    IdNhanVien = dto.IdNhanVien,
                    NgayThue = DateTime.Now,
                    TrangThai = "Đang Thuê",
                    TongTienCoc = tongCoc
                };
                _context.PhieuThueSachs.Add(phieuThue);
                await _context.SaveChangesAsync();

                // 3. Xử lý Chi Tiết Phiếu Thuê và Sách
                foreach (var sachThue in dto.SachCanThue)
                {
                    var sach = await _context.Sachs.FindAsync(sachThue.IdSach);
                    if (sach == null || sach.SoLuongHienCo <= 0)
                    {
                        await transaction.RollbackAsync();
                        return Conflict($"Sách '{sach?.TenSach ?? "ID: " + sachThue.IdSach}' đã hết hàng.");
                    }
                    sach.SoLuongHienCo--;
                    var chiTiet = new ChiTietPhieuThue
                    {
                        IdPhieuThueSach = phieuThue.IdPhieuThueSach,
                        IdSach = sachThue.IdSach,
                        NgayHenTra = dto.NgayHenTra,
                        TienCoc = sachThue.TienCoc,
                        TienPhatTraTre = null,
                        NgayTraThucTe = null
                    };
                    _context.ChiTietPhieuThues.Add(chiTiet);
                }

                // 4. SỬA LỖI LOGIC: Xóa tích điểm khi tạo phiếu

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { IdPhieuThueSach = phieuThue.IdPhieuThueSach, TongTienCoc = tongCoc });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message} \nChi tiết: {ex.InnerException?.Message}");
            }
        }

        /// <summary>
        /// CẢI TIẾN (F2) & SỬA LỖI LOGIC (Tích điểm)
        /// </summary>
        [HttpPost("return")]
        public async Task<IActionResult> ReturnSach([FromBody] TraSachRequestDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var settings = await GetSettingsInternal();
                var phieu = await _context.PhieuThueSachs
                    .Include(p => p.ChiTietPhieuThues)
                    .FirstOrDefaultAsync(p => p.IdPhieuThueSach == dto.IdPhieuThueSach);

                if (phieu == null) return NotFound("Không tìm thấy phiếu thuê.");
                var khach = await _context.KhachHangs.FindAsync(phieu.IdKhachHang);
                if (khach == null) return NotFound("Không tìm thấy khách hàng.");

                decimal totalPhat = 0;
                decimal totalCoc = 0;
                decimal totalPhiThue = 0;
                int sachDaTra = 0;
                var now = DateTime.Now;

                var phieuTra = new PhieuTraSach
                {
                    IdPhieuThueSach = phieu.IdPhieuThueSach,
                    IdNhanVien = dto.IdNhanVien,
                    NgayTra = now,
                    ChiTietPhieuTras = new List<ChiTietPhieuTra>()
                };

                foreach (int idSach in dto.IdSachs)
                {
                    var ct = phieu.ChiTietPhieuThues.FirstOrDefault(c => c.IdSach == idSach && c.NgayTraThucTe == null);
                    if (ct == null) continue;

                    var sach = await _context.Sachs.FindAsync(idSach);
                    if (sach != null) sach.SoLuongHienCo++;

                    ct.NgayTraThucTe = now;

                    decimal tienPhat = 0;
                    if (ct.NgayHenTra < now.Date)
                    {
                        int daysLate = (int)(now.Date - ct.NgayHenTra).TotalDays;
                        tienPhat = daysLate * settings.PhiTraTreMoiNgay;
                    }
                    ct.TienPhatTraTre = tienPhat;

                    totalPhat += tienPhat;
                    totalCoc += ct.TienCoc;
                    totalPhiThue += settings.PhiThue;
                    sachDaTra++;

                    phieuTra.ChiTietPhieuTras.Add(new ChiTietPhieuTra
                    {
                        IdSach = idSach,
                        TienPhat = tienPhat
                    });
                }

                if (sachDaTra == 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest("Không có sách nào hợp lệ để trả.");
                }

                if (!phieu.ChiTietPhieuThues.Any(ct => ct.NgayTraThucTe == null))
                {
                    phieu.TrangThai = "Đã Trả";
                }

                // SỬA LỖI LOGIC TÍCH ĐIỂM
                // Chỉ cộng điểm khi phiếu được trả (toàn bộ hoặc một phần)
                int diem = settings.DiemPhieuThue;
                khach.DiemTichLuy += diem;

                phieuTra.TongPhiThue = totalPhiThue;
                phieuTra.TongTienPhat = totalPhat;
                phieuTra.TongTienCocHoan = totalCoc;
                phieuTra.DiemTichLuy = diem; // Lưu lại số điểm đã cộng
                _context.PhieuTraSachs.Add(phieuTra);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var response = new TraSachResponseDto
                {
                    IdPhieuTra = phieuTra.IdPhieuTra,
                    SoSachDaTra = sachDaTra,
                    TongPhiThue = totalPhiThue,
                    TongTienPhat = totalPhat,
                    TongTienCoc = totalCoc,
                    TongHoanTra = totalCoc - totalPhiThue - totalPhat, // Sửa: Cọc - (Phí + Phạt)
                    DiemTichLuy = diem
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message} \nChi tiết: {ex.InnerException?.Message}");
            }
        }


        /// <summary>
        /// CẢI TIẾN (F7): Tìm kiếm Lịch sử trả
        /// </summary>
        [HttpGet("phieutra")]
        public async Task<IActionResult> GetPhieuTra([FromQuery] string? search)
        {
            var query = _context.PhieuTraSachs.Include(pt => pt.NhanVien).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                int.TryParse(search, out int phieuId);
                query = query.Where(pt =>
                    pt.IdPhieuTra == phieuId ||
                    pt.IdPhieuThueSach == phieuId
                );
            }

            var phieuTra = await query
                .OrderByDescending(pt => pt.NgayTra)
                .Take(100)
                .Select(pt => new PhieuTraGridDto
                {
                    IdPhieuTra = pt.IdPhieuTra,
                    IdPhieuThueSach = pt.IdPhieuThueSach,
                    NgayTra = pt.NgayTra,
                    TenNhanVien = pt.NhanVien.HoTen,
                    TongHoanTra = pt.TongTienCocHoan - pt.TongTienPhat - pt.TongPhiThue
                })
                .ToListAsync();

            return Ok(phieuTra);
        }


        /// <summary>
        /// CẢI TIẾN (F-Email): Chỉ gửi Mail (Hành động chủ động)
        /// </summary>
        [HttpPost("send-reminder/{idPhieu}")]
        public async Task<IActionResult> SendReminder(int idPhieu)
        {
            var phieu = await _context.PhieuThueSachs
                .Include(p => p.KhachHang)
                .Include(p => p.ChiTietPhieuThues)
                .FirstOrDefaultAsync(p => p.IdPhieuThueSach == idPhieu);

            if (phieu == null) return NotFound();

            var khach = phieu.KhachHang;
            if (string.IsNullOrEmpty(khach.Email))
                return BadRequest("Khách hàng này không có Email.");

            var sachChuaTra = phieu.ChiTietPhieuThues
                .Where(ct => ct.NgayTraThucTe == null && ct.NgayHenTra < DateTime.Now.AddDays(2))
                .ToList();

            if (!sachChuaTra.Any())
                return BadRequest("Khách hàng không có sách nào sắp trễ hạn.");

            var ngayHenTra = sachChuaTra.Min(s => s.NgayHenTra);
            var noiDung = $"Xin chào {khach.HoTen},<br><br>Cafebook trân trọng thông báo bạn có {sachChuaTra.Count} cuốn sách sắp/đã trễ hạn (hạn trả: {ngayHenTra:dd/MM/yyyy}). Vui lòng kiểm tra và trả sách sớm. <br><br>Cảm ơn bạn.";
            var subject = $"[Cafebook] Thông báo trễ hạn thuê sách (Phiếu PT{idPhieu})";

            try
            {
                // 1. Gửi Email (Viết trực tiếp)
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpSettings["Username"], smtpSettings["FromName"]),
                    Subject = subject,
                    Body = noiDung,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(khach.Email);

                using var smtpClient = new SmtpClient(smtpSettings["Host"], int.Parse(smtpSettings["Port"]))
                {
                    Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                    EnableSsl = bool.Parse(smtpSettings["EnableSsl"])
                };
                await smtpClient.SendMailAsync(mailMessage);

                // 2. XÓA LOGIC TẠO THÔNG BÁO (Theo yêu cầu)

                return Ok(new { Message = $"Đã gửi email nhắc nhở đến {khach.Email}." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi gửi email: {ex.Message}");
            }
        }

        /// <summary>
        /// CẢI TIẾN (F-Email): Gửi email hàng loạt (Hành động chủ động)
        /// </summary>
        [HttpPost("send-all-reminders")]
        public async Task<IActionResult> SendAllReminders()
        {
            var phieuSapTre = await _context.ChiTietPhieuThues
                .Where(ct => ct.NgayTraThucTe == null && ct.NgayHenTra.Date == DateTime.Today.AddDays(1))
                .Include(ct => ct.PhieuThueSach.KhachHang)
                .GroupBy(ct => ct.PhieuThueSach) // Nhóm theo Phiếu Thuê
                .ToListAsync();

            if (!phieuSapTre.Any())
            {
                return Ok(new { Message = "Không có khách hàng nào sắp trễ hạn vào ngày mai." });
            }

            // Lấy cài đặt SMTP
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            using var smtpClient = new SmtpClient(smtpSettings["Host"], int.Parse(smtpSettings["Port"]))
            {
                Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"])
            };

            int count = 0;
            foreach (var group in phieuSapTre)
            {
                var khach = group.Key.KhachHang;
                var sachList = group.ToList();
                var ngayHenTra = sachList.Min(s => s.NgayHenTra);

                if (!string.IsNullOrEmpty(khach.Email))
                {
                    try
                    {
                        // 1. Gửi Email
                        var noiDung = $"Xin chào {khach.HoTen},<br><br>Cafebook trân trọng thông báo bạn có {sachList.Count} cuốn sách sẽ đến hạn trả vào ngày mai ({ngayHenTra:dd/MM/yyyy}). Vui lòng kiểm tra và trả sách đúng hạn.<br><br>Cảm ơn bạn.";
                        var subject = "[Cafebook] Thông báo nhắc hạn trả sách";

                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress(smtpSettings["Username"], smtpSettings["FromName"]),
                            Subject = subject,
                            Body = noiDung,
                            IsBodyHtml = true,
                        };
                        mailMessage.To.Add(khach.Email);

                        await smtpClient.SendMailAsync(mailMessage);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        // Ghi log lỗi nhưng vẫn tiếp tục
                        Console.WriteLine($"Lỗi gửi mail cho {khach.Email}: {ex.Message}");
                    }
                }
            }

            // 2. XÓA LOGIC TẠO THÔNG BÁO (Theo yêu cầu)

            return Ok(new { Message = $"Đã gửi {count} email nhắc nhở hàng loạt." });
        }

        // Lấy dữ liệu cho phiếu in (Thuê)
        [HttpGet("print-data/{idPhieu}")]
        public async Task<IActionResult> GetPrintData(int idPhieu)
        {
            var phieu = await _context.PhieuThueSachs
                .Include(p => p.KhachHang)
                .Include(p => p.NhanVien)
                .Include(p => p.ChiTietPhieuThues).ThenInclude(ct => ct.Sach)
                .FirstOrDefaultAsync(p => p.IdPhieuThueSach == idPhieu);

            if (phieu == null) return NotFound();

            var settings = await _context.CaiDats.ToListAsync();
            var settingsThue = await GetSettingsInternal();

            var dto = new PhieuThuePrintDto
            {
                IdPhieu = $"PT{phieu.IdPhieuThueSach:D6}",
                TenQuan = settings.FirstOrDefault(c => c.TenCaiDat == "TenQuan")?.GiaTri ?? "Cafebook",
                DiaChiQuan = settings.FirstOrDefault(c => c.TenCaiDat == "DiaChi")?.GiaTri ?? "N/A",
                SdtQuan = settings.FirstOrDefault(c => c.TenCaiDat == "SoDienThoai")?.GiaTri ?? "N/A",
                NgayTao = phieu.NgayThue,
                TenNhanVien = phieu.NhanVien.HoTen,
                TenKhachHang = phieu.KhachHang.HoTen,
                SdtKhachHang = phieu.KhachHang.SoDienThoai ?? "N/A",
                NgayHenTra = phieu.ChiTietPhieuThues.Min(ct => ct.NgayHenTra),
                ChiTiet = phieu.ChiTietPhieuThues.Select(ct => new ChiTietPrintDto
                {
                    TenSach = ct.Sach.TenSach,
                    TienCoc = ct.TienCoc
                }).ToList(),
                TongTienCoc = phieu.TongTienCoc,
                TongPhiThue = phieu.ChiTietPhieuThues.Count * settingsThue.PhiThue
            };

            return Ok(dto);
        }

        /// <summary>
        /// SỬA LỖI (EF Core): Sửa lỗi 'Walking back include tree'
        /// </summary>
        [HttpGet("print-data/tra/{idPhieuTra}")]
        public async Task<IActionResult> GetPrintDataTra(int idPhieuTra)
        {
            // SỬA LỖI: Đơn giản hóa Include
            var phieuTra = await _context.PhieuTraSachs
                .Include(pt => pt.NhanVien)
                .Include(pt => pt.PhieuThueSach.KhachHang)
                // THÊM Include ChiTietPhieuThues ở đây
                .Include(pt => pt.PhieuThueSach.ChiTietPhieuThues)
                .Include(pt => pt.ChiTietPhieuTras).ThenInclude(ct => ct.Sach)
                // XÓA dòng Include bị lỗi
                // .Include(pt => pt.ChiTietPhieuTras).ThenInclude(ct => ct.PhieuTraSach.PhieuThueSach.ChiTietPhieuThues) 
                .FirstOrDefaultAsync(pt => pt.IdPhieuTra == idPhieuTra);

            if (phieuTra == null) return NotFound();

            var settings = await _context.CaiDats.ToListAsync();
            var khach = phieuTra.PhieuThueSach.KhachHang;

            var dto = new PhieuTraPrintDto
            {
                IdPhieuTra = $"PTR{phieuTra.IdPhieuTra:D6}",
                IdPhieuThue = $"PT{phieuTra.IdPhieuThueSach:D6}",
                TenQuan = settings.FirstOrDefault(c => c.TenCaiDat == "TenQuan")?.GiaTri ?? "Cafebook",
                DiaChiQuan = settings.FirstOrDefault(c => c.TenCaiDat == "DiaChi")?.GiaTri ?? "N/A",
                SdtQuan = settings.FirstOrDefault(c => c.TenCaiDat == "SoDienThoai")?.GiaTri ?? "N/A",
                NgayTra = phieuTra.NgayTra,
                TenNhanVien = phieuTra.NhanVien.HoTen,
                TenKhachHang = khach.HoTen,
                SdtKhachHang = khach.SoDienThoai ?? "N/A",
                DiemTichLuy = phieuTra.DiemTichLuy,

                ChiTiet = phieuTra.ChiTietPhieuTras.Select(ct => new ChiTietTraPrintDto
                {
                    TenSach = ct.Sach.TenSach,
                    TienPhat = ct.TienPhat,
                    // Logic Select này giờ sẽ hoạt động
                    TienCoc = phieuTra.PhieuThueSach.ChiTietPhieuThues
                                .FirstOrDefault(cts => cts.IdSach == ct.IdSach)?.TienCoc ?? 0
                }).ToList(),

                TongTienCoc = phieuTra.TongTienCocHoan,
                TongPhiThue = phieuTra.TongPhiThue,
                TongTienPhat = phieuTra.TongTienPhat,
                TongHoanTra = phieuTra.TongTienCocHoan - phieuTra.TongPhiThue - phieuTra.TongTienPhat
            };

            return Ok(dto);
        }

        // (GetSettingsInternal giữ nguyên)
        private async Task<CaiDatThueSachDto> GetSettingsInternal()
        {
            var settings = await _context.CaiDats
                .Where(c => c.TenCaiDat.StartsWith("Sach_") || c.TenCaiDat.StartsWith("DiemTichLuy_"))
                .ToListAsync();

            return new CaiDatThueSachDto
            {
                PhiThue = decimal.Parse(settings.FirstOrDefault(c => c.TenCaiDat == "Sach_PhiThue")?.GiaTri ?? "5000"),
                PhiTraTreMoiNgay = decimal.Parse(settings.FirstOrDefault(c => c.TenCaiDat == "Sach_PhiTraTreMoiNgay")?.GiaTri ?? "2000"),
                SoNgayMuonToiDa = int.Parse(settings.FirstOrDefault(c => c.TenCaiDat == "Sach_SoNgayMuonToiDa")?.GiaTri ?? "7"),
                DiemPhieuThue = int.Parse(settings.FirstOrDefault(c => c.TenCaiDat == "Sach_DiemPhieuThue")?.GiaTri ?? "1"),
                PointToVND = decimal.Parse(settings.FirstOrDefault(c => c.TenCaiDat == "DiemTichLuy_DoiVND")?.GiaTri ?? "1000")
            };
        }
    }
}