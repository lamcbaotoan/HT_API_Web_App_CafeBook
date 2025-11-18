// Tập tin: CafebookApi/Services/AiToolService.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions; // Thêm
using System.Threading.Tasks;

namespace CafebookApi.Services
{
    public class AiToolService
    {
        private readonly CafebookDbContext _context;
        private const int SlotDurationHours = 2; // Yêu cầu 2 giờ

        public AiToolService(CafebookDbContext context)
        {
            _context = context;
        }

        // --- CÔNG CỤ CHO LUỒNG 1 (GUEST) ---

        /// <summary>
        /// (Tool 1) Lấy thông tin chung của quán
        /// </summary>
        public async Task<object> GetThongTinChungAsync()
        {
            var settings = await _context.CaiDats
                .AsNoTracking()
                .Where(c => c.TenCaiDat.StartsWith("LienHe_") || c.TenCaiDat.StartsWith("Wifi_"))
                .Select(c => new { c.TenCaiDat, c.GiaTri })
                .ToListAsync();

            var gioMoCua = settings.FirstOrDefault(s => s.TenCaiDat == "LienHe_GioMoCua")?.GiaTri ?? "6:00 - 22:00";
            var diaChi = settings.FirstOrDefault(s => s.TenCaiDat == "LienHe_DiaChi")?.GiaTri ?? "Chưa cập nhật";
            var wifi = settings.FirstOrDefault(s => s.TenCaiDat == "Wifi_MatKhau")?.GiaTri ?? "Không có";

            return new { GioMoCua = gioMoCua, DiaChi = diaChi, Wifi = wifi };
        }

        /// <summary>
        /// (Tool 2) Kiểm tra tình trạng bàn trống
        /// </summary>
        public async Task<object> KiemTraBanTrongAsync(int soNguoi)
        {
            var banTrong = await _context.Bans
                .AsNoTracking()
                .Where(b => b.TrangThai == "Trống" && b.SoGhe >= soNguoi)
                .OrderBy(b => b.SoGhe)
                .Select(b => new
                {
                    IdBan = b.IdBan,
                    SoBan = b.SoBan,
                    SoGhe = b.SoGhe,
                    TenKhuVuc = b.KhuVuc != null ? b.KhuVuc.TenKhuVuc : "chung"
                })
                .Take(5)
                .ToListAsync();

            // Sửa lỗi API 400: Bọc mảng vào một object
            return new { banTimThay = banTrong };
        }

        /// <summary>
        /// (Tool 3) Kiểm tra tình trạng món ăn (Kiểm tra tồn kho nguyên liệu)
        /// </summary>
        public async Task<object> KiemTraSanPhamAsync(string tenSanPham)
        {
            var sanPham = await _context.SanPhams
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenSanPham.Contains(tenSanPham));

            if (sanPham == null)
            {
                return new { TrangThai = "NotFound", TenTimKiem = tenSanPham };
            }

            if (sanPham.TrangThaiKinhDoanh == false)
            {
                return new { TrangThai = "NgungKinhDoanh", TenSanPham = sanPham.TenSanPham };
            }

            // (Logic kiểm kho nâng cao đã thêm ở bước trước)
            var dinhLuongList = await _context.DinhLuongs
                .AsNoTracking()
                .Include(d => d.NguyenLieu)
                .Include(d => d.DonViSuDung)
                .Where(d => d.IdSanPham == sanPham.IdSanPham)
                .ToListAsync();

            if (!dinhLuongList.Any())
            {
                return new { TrangThai = "ConHang", TenSanPham = sanPham.TenSanPham, GiaBan = sanPham.GiaBan, GhiChu = "Sản phẩm không cần định lượng" };
            }

            var nguyenLieuHetHang = new List<string>();
            foreach (var item in dinhLuongList)
            {
                if (item.NguyenLieu == null || item.DonViSuDung == null) continue;
                decimal luongCanDungDaQuyDoi = item.SoLuongSuDung * item.DonViSuDung.GiaTriQuyDoi;
                if (item.NguyenLieu.TonKho < luongCanDungDaQuyDoi)
                {
                    nguyenLieuHetHang.Add(item.NguyenLieu.TenNguyenLieu);
                }
            }

            if (nguyenLieuHetHang.Any())
            {
                return new
                {
                    TrangThai = "TamHetHang",
                    TenSanPham = sanPham.TenSanPham,
                    GiaBan = sanPham.GiaBan,
                    GhiChu = $"Tạm hết hàng do thiếu: {string.Join(", ", nguyenLieuHetHang)}"
                };
            }

            return new { TrangThai = "ConHang", TenSanPham = sanPham.TenSanPham, GiaBan = sanPham.GiaBan };
        }


        /// <summary>
        /// (Tool 4) Kiểm tra sách
        /// </summary>
        public async Task<object> KiemTraSachAsync(string tenSach)
        {
            var sach = await _context.Sachs
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenSach.Contains(tenSach));

            if (sach == null)
            {
                return new { TrangThai = "NotFound", TenTimKiem = tenSach };
            }

            if (sach.SoLuongHienCo > 0)
            {
                // Trả về ID Sách để dùng cho tool gợi ý
                return new { TrangThai = "ConHang", IdSach = sach.IdSach, TenSach = sach.TenSach, SoLuongHienCo = sach.SoLuongHienCo, ViTri = sach.ViTri };
            }
            else
            {
                return new { TrangThai = "HetHang", IdSach = sach.IdSach, TenSach = sach.TenSach };
            }
        }

        // === NÂNG CẤP MỚI: CÁC TOOL CHO KHÁCH ĐÃ ĐĂNG NHẬP ===

        /// <summary>
        /// (Tool 5) Lấy tổng quan tài khoản (thay thế GetThongTinKhachHangAsync)
        /// Gộp 6 yêu cầu: Điểm, Tổng chi, Tổng hóa đơn, Lịch sử (3 loại)
        /// </summary>
        public async Task<object> GetTongQuanTaiKhoanAsync(int idKhachHang)
        {
            var khachHang = await _context.KhachHangs.AsNoTracking()
                .FirstOrDefaultAsync(k => k.IdKhachHang == idKhachHang);
            if (khachHang == null) return new { TrangThai = "NotFound" };

            // Lấy lịch sử (3 loại)
            var hoaDons = await _context.HoaDons
                .AsNoTracking()
                .Where(h => h.IdKhachHang == idKhachHang && h.TrangThai == "Đã thanh toán")
                .OrderByDescending(h => h.ThoiGianTao)
                .ToListAsync(); // Lấy hết để tính Sum

            var donHangGiaoGanNhat = hoaDons
                .Where(h => h.LoaiHoaDon == "Giao hàng")
                .FirstOrDefault(); // Lấy đơn giao hàng gần nhất

            var phieuThueGanNhat = await _context.PhieuThueSachs
                .AsNoTracking()
                .Where(p => p.IdKhachHang == idKhachHang)
                .OrderByDescending(p => p.NgayThue)
                .Select(p => new { p.IdPhieuThueSach, p.NgayThue, p.TrangThai, p.TongTienCoc })
                .FirstOrDefaultAsync();

            var datBanGanNhat = await _context.PhieuDatBans
                .AsNoTracking()
                .Include(p => p.Ban)
                .Where(p => p.IdKhachHang == idKhachHang)
                .OrderByDescending(p => p.ThoiGianDat)
                .Select(p => new { p.IdPhieuDatBan, p.Ban.SoBan, p.ThoiGianDat, p.TrangThai })
                .FirstOrDefaultAsync();

            return new
            {
                TrangThai = "Found",
                // Thông tin cá nhân
                HoTen = khachHang.HoTen,
                SoDienThoai = khachHang.SoDienThoai,
                Email = khachHang.Email,
                // Tổng quan (6 yêu cầu)
                DiemTichLuy = khachHang.DiemTichLuy,
                TongSoHoaDon = hoaDons.Count,
                TongChiTieu = hoaDons.Sum(h => h.ThanhTien),
                // Lịch sử gần nhất
                DonHangGanNhat = donHangGiaoGanNhat != null ? new { donHangGiaoGanNhat.IdHoaDon, donHangGiaoGanNhat.ThoiGianTao, donHangGiaoGanNhat.TrangThaiGiaoHang, donHangGiaoGanNhat.ThanhTien } : null,
                PhieuThueGanNhat = phieuThueGanNhat,
                DatBanGanNhat = datBanGanNhat
            };
        }


        /// <summary>
        /// (Tool 6) Ghi phiếu đặt bàn vào CSDL
        /// </summary>
        public async Task<object> DatBanThucSuAsync(int idBan, int soNguoi, DateTime thoiGianDat, string hoTen, string soDienThoai, string email, string? ghiChu)
        {
            // 1. Kiểm tra lại thời gian (logic C# này là chuẩn)
            var openingHours = await GetAndParseOpeningHours();
            if (thoiGianDat < DateTime.Now.AddMinutes(10))
            {
                return new { TrangThai = "Error", Message = $"Giờ đặt quá gần. Vui lòng chọn thời gian sau {DateTime.Now.AddMinutes(10):HH:mm}." };
            }
            if (!IsTimeValid(thoiGianDat, openingHours))
            {
                return new { TrangThai = "Error", Message = $"Giờ đặt ({thoiGianDat:HH:mm}) nằm ngoài giờ mở cửa ({openingHours.Open:hh\\:mm} - {openingHours.Close:hh\\:mm})." };
            }

            // 2. Kiểm tra xung đột
            bool isConflict = await _context.PhieuDatBans.AnyAsync(p =>
                p.IdBan == idBan &&
                (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận" || p.TrangThai == "Khách đã đến") &&
                thoiGianDat < p.ThoiGianDat.AddHours(SlotDurationHours) &&
                p.ThoiGianDat < thoiGianDat.AddHours(SlotDurationHours)
            );

            if (isConflict)
            {
                return new { TrangThai = "Error", Message = "Rất tiếc, bàn này vừa có người đặt vào khung giờ bạn chọn. Vui lòng chọn bàn khác." };
            }

            // 3. Tìm hoặc Tạo Khách Hàng
            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.SoDienThoai == soDienThoai);
            if (khachHang == null)
            {
                khachHang = new KhachHang
                {
                    HoTen = hoTen,
                    SoDienThoai = soDienThoai,
                    Email = email,
                    NgayTao = DateTime.Now,
                    DiemTichLuy = 0,
                    BiKhoa = false,
                    TenDangNhap = soDienThoai,
                    MatKhau = Guid.NewGuid().ToString("N")[..8],
                    TaiKhoanTam = true
                };
                _context.KhachHangs.Add(khachHang);
                await _context.SaveChangesAsync();
            }

            // 4. Tạo phiếu đặt bàn
            var phieuMoi = new PhieuDatBan
            {
                IdBan = idBan,
                IdKhachHang = khachHang.IdKhachHang,
                HoTenKhach = hoTen,
                SdtKhach = soDienThoai,
                ThoiGianDat = thoiGianDat,
                SoLuongKhach = soNguoi,
                GhiChu = ghiChu,
                TrangThai = "Chờ xác nhận"
            };
            _context.PhieuDatBans.Add(phieuMoi);

            // 5. Tạo thông báo cho nhân viên
            var banInfo = await _context.Bans.FindAsync(idBan);
            var thongBao = new ThongBao
            {
                NoiDung = $"[AI] Đơn đặt bàn mới: {hoTen} - Bàn {banInfo?.SoBan} lúc {thoiGianDat:HH:mm dd/MM}",
                LoaiThongBao = "DatBan",
                IdLienQuan = phieuMoi.IdPhieuDatBan,
                ThoiGianTao = DateTime.Now,
                DaXem = false
            };
            _context.ThongBaos.Add(thongBao);

            await _context.SaveChangesAsync();

            return new { TrangThai = "Success", IdPhieuDatBan = phieuMoi.IdPhieuDatBan, TenBan = banInfo?.SoBan, HoTen = hoTen, ThoiGian = thoiGianDat };
        }

        /// <summary>
        /// (Tool 7) Gợi ý sách dựa trên BẢNG ĐỀ XUẤT (ngẫu nhiên)
        /// </summary>
        public async Task<object> GetGoiYSachNgauNhienAsync()
        {
            var suggestions = await _context.DeXuatSachs
                .Include(d => d.SachDeXuat)
                .GroupBy(d => d.SachDeXuat)
                .OrderByDescending(g => g.Count())
                .Select(g => new
                {
                    IdSach = g.Key.IdSach,
                    TenSach = g.Key.TenSach,
                })
                .Take(3) // Lấy 3 cuốn
                .ToListAsync();

            if (!suggestions.Any())
            {
                return new { TrangThai = "Error", Message = "Không tìm thấy gợi ý nào." };
            }

            return new { TrangThai = "Success", GoiY = suggestions };
        }

        /// <summary>
        /// (Tool 8) Theo dõi chi tiết 1 đơn hàng
        /// </summary>
        public async Task<object> TheoDoiDonHangAsync(int idHoaDon, int idKhachHang)
        {
            var hoaDon = await _context.HoaDons
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon && h.IdKhachHang == idKhachHang);

            if (hoaDon == null)
            {
                return new { TrangThai = "NotFound", Message = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem." };
            }

            // Logic mô phỏng tracking (copy từ KhachHangProfileController)
            var trackingEvents = new List<object>();
            string currentStatus = hoaDon.TrangThaiGiaoHang ?? "Chờ xác nhận";

            // Helper để thêm event và đánh dấu IsCurrent
            void AddEvent(DateTime time, string status, string desc, bool isCurrent = false)
            {
                trackingEvents.Add(new { Timestamp = time, Status = status, Description = desc, IsCurrent = isCurrent });
            }

            AddEvent(hoaDon.ThoiGianTao, "Đơn hàng đã đặt", "Đơn hàng của bạn đã được đặt thành công.", currentStatus == "Chờ xác nhận");

            if (currentStatus == "Đang chuẩn bị" || currentStatus == "Đang giao" || currentStatus == "Hoàn thành")
            {
                AddEvent(hoaDon.ThoiGianTao.AddMinutes(15), "Đơn hàng được xác nhận", "Shop đang chuẩn bị hàng.", currentStatus == "Đang chuẩn bị");
            }
            if (currentStatus == "Đang giao" || currentStatus == "Hoàn thành")
            {
                AddEvent(hoaDon.ThoiGianTao.AddMinutes(45), "Đã giao cho ĐVVC", "Đơn hàng đã được bàn giao cho đơn vị vận chuyển.", currentStatus == "Đang giao");
            }
            if (currentStatus == "Hoàn thành")
            {
                AddEvent(hoaDon.ThoiGianThanhToan ?? hoaDon.ThoiGianTao.AddHours(2), "Giao hàng thành công", "Đơn hàng đã được giao đến bạn.", true);
            }

            return new
            {
                TrangThai = "Found",
                IdHoaDon = hoaDon.IdHoaDon,
                TrangThaiHienTai = currentStatus,
                TrackingEvents = trackingEvents // Đã được sắp xếp khi thêm
            };
        }


        // === HELPER (Copy từ DatBanWebController.cs) ===
        private class OpeningHours
        {
            public TimeSpan Open { get; set; } = new TimeSpan(6, 0, 0);
            public TimeSpan Close { get; set; } = new TimeSpan(23, 0, 0);
        }

        private async Task<OpeningHours> GetAndParseOpeningHours()
        {
            var setting = await _context.CaiDats
                .FirstOrDefaultAsync(cd => cd.TenCaiDat == "LienHe_GioMoCua");
            string settingValue = (setting != null && !string.IsNullOrEmpty(setting.GiaTri)) ? setting.GiaTri : "06:00 - 23:00";

            var hours = new OpeningHours();
            try
            {
                var match = Regex.Match(settingValue, @"(\d{2}:\d{2})\s*-\s*(\d{2}:\d{2})");
                if (match.Success)
                {
                    if (TimeSpan.TryParse(match.Groups[1].Value, out TimeSpan open)) hours.Open = open;
                    if (TimeSpan.TryParse(match.Groups[2].Value, out TimeSpan close)) hours.Close = close;
                }
            }
            catch { }
            return hours;
        }

        private bool IsTimeValid(DateTime thoiGianDat, OpeningHours hours)
        {
            var timeOfDay = thoiGianDat.TimeOfDay;
            return timeOfDay >= hours.Open && timeOfDay < hours.Close;
        }
    }
}