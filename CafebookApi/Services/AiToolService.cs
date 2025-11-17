// Tập tin: CafebookApi/Services/AiToolService.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Services
{
    /// <summary>
    /// Dịch vụ này chứa các hàm nghiệp vụ (Tools) mà AI có thể gọi
    /// để truy vấn cơ sở dữ liệu.
    /// </summary>
    public class AiToolService
    {
        private readonly CafebookDbContext _context;

        public AiToolService(CafebookDbContext context)
        {
            _context = context;
        }

        // --- CÔNG CỤ CHO LUỒNG 1 (GUEST) ---

        /// <summary>
        /// (Tool 1) Lấy thông tin chung của quán
        /// </summary>
        public async Task<string> GetThongTinChungAsync()
        {
            var settings = await _context.CaiDats
                .AsNoTracking()
                .Where(c => c.TenCaiDat.StartsWith("LienHe_") || c.TenCaiDat.StartsWith("Wifi_"))
                .Select(c => new { c.TenCaiDat, c.GiaTri })
                .ToListAsync();

            var gioMoCua = settings.FirstOrDefault(s => s.TenCaiDat == "LienHe_GioMoCua")?.GiaTri ?? "6:00 - 22:00";
            var diaChi = settings.FirstOrDefault(s => s.TenCaiDat == "LienHe_DiaChi")?.GiaTri ?? "Chưa cập nhật";
            var wifi = settings.FirstOrDefault(s => s.TenCaiDat == "Wifi_MatKhau")?.GiaTri ?? "Không có";

            return $"Thông tin quán: Giờ mở cửa: {gioMoCua}. Địa chỉ: {diaChi}. Mật khẩu Wifi: {wifi}.";
        }

        /// <summary>
        /// (Tool 2) Kiểm tra tình trạng bàn trống
        /// </summary>
        public async Task<string> KiemTraBanTrongAsync(int soNguoi)
        {
            var banTrong = await _context.Bans
                .AsNoTracking()
                .Where(b => b.TrangThai == "Trống" && b.SoGhe >= soNguoi)
                .OrderBy(b => b.SoGhe)
                // Tập tin: AiToolService.cs

                .Select(b => new { b.SoBan, b.SoGhe, TenKhuVuc = b.KhuVuc != null ? b.KhuVuc.TenKhuVuc : "chung" })
                .Take(5)
                .ToListAsync();

            if (!banTrong.Any())
            {
                return "Rất tiếc, hiện tại các bàn trống không đủ chỗ cho yêu cầu của bạn.";
            }

            var ketQua = $"Tìm thấy {banTrong.Count} bàn phù hợp: \n";
            foreach (var ban in banTrong)
            {
                ketQua += $"- Bàn {ban.SoBan} ({ban.SoGhe} ghế) tại khu {ban.TenKhuVuc ?? "chung"}\n";
            }
            return ketQua;
        }

        /// <summary>
        /// (Tool 3) Kiểm tra tình trạng món ăn/thức uống
        /// </summary>
        public async Task<string> KiemTraSanPhamAsync(string tenSanPham)
        {
            var sanPham = await _context.SanPhams
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenSanPham.Contains(tenSanPham));

            if (sanPham == null)
            {
                return $"Không tìm thấy sản phẩm nào có tên giống '{tenSanPham}'.";
            }

            if (sanPham.TrangThaiKinhDoanh == false)
            {
                return $"Món '{sanPham.TenSanPham}' hiện đã ngừng kinh doanh.";
            }

            // Giả định: kiểm tra nguyên liệu (logic phức tạp)
            // Ở đây chúng ta chỉ kiểm tra trạng thái kinh doanh
            return $"Món '{sanPham.TenSanPham}' hiện VẪN CÒN HÀNG và đang kinh doanh.";
        }

        /// <summary>
        /// (Tool 4) Kiểm tra sách
        /// </summary>
        public async Task<string> KiemTraSachAsync(string tenSach)
        {
            var sach = await _context.Sachs
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenSach.Contains(tenSach));

            if (sach == null)
            {
                return $"Không tìm thấy sách nào có tên giống '{tenSach}'.";
            }

            if (sach.SoLuongHienCo > 0)
            {
                return $"Sách '{sach.TenSach}' hiện còn {sach.SoLuongHienCo} quyển trên kệ.";
            }
            else
            {
                return $"Rất tiếc, sách '{sach.TenSach}' hiện đã được mượn hết.";
            }
        }

        // --- CÔNG CỤ CHO LUỒNG 2 (KHÁCH HÀNG) ---

        /// <summary>
        /// (Tool 5) Lấy thông tin khách hàng (điểm, hóa đơn)
        /// </summary>
        public async Task<string> GetThongTinKhachHangAsync(int idKhachHang)
        {
            var khachHang = await _context.KhachHangs.AsNoTracking()
                .FirstOrDefaultAsync(k => k.IdKhachHang == idKhachHang);

            if (khachHang == null) return "Lỗi: Không tìm thấy khách hàng.";

            var hoaDonGanNhat = await _context.HoaDons
                .AsNoTracking()
                .Where(h => h.IdKhachHang == idKhachHang)
                .OrderByDescending(h => h.ThoiGianTao)
                .Select(h => new { h.ThanhTien, h.ThoiGianTao })
                .FirstOrDefaultAsync();

            var phieuThue = await _context.PhieuThueSachs
                .AsNoTracking()
                .Include(p => p.ChiTietPhieuThues)
                .Where(p => p.IdKhachHang == idKhachHang && p.TrangThai == "Đang mượn")
                .CountAsync();

            string ketQua = $"Thông tin của bạn (ID: {idKhachHang}): \n" +
                            $"- Tên: {khachHang.HoTen} \n" +
                            $"- Điểm tích lũy: {khachHang.DiemTichLuy} điểm. \n" +
                            $"- Hóa đơn gần nhất: {hoaDonGanNhat?.ThanhTien} lúc {hoaDonGanNhat?.ThoiGianTao}. \n" +
                            $"- Sách đang mượn: {phieuThue} quyển.";

            return ketQua;
        }
    }
}