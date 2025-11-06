using CafebookModel.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafebookApi.Data
{
    public class CafebookDbContext : DbContext
    {
        public CafebookDbContext(DbContextOptions<CafebookDbContext> options)
            : base(options)
        {
        }

        // --- Khai báo tất cả các Bảng (DbSet) ---
        public DbSet<KhuVuc> KhuVucs { get; set; }
        public DbSet<Ban> Bans { get; set; }
        public DbSet<NguoiGiaoHang> NguoiGiaoHangs { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<DanhMuc> DanhMucs { get; set; }
        public DbSet<SanPham> SanPhams { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public DbSet<PhuThu> PhuThus { get; set; }
        public DbSet<ChiTietPhuThuHoaDon> ChiTietPhuThuHoaDons { get; set; }
        public DbSet<NhatKyHuyMon> NhatKyHuyMons { get; set; }
        public DbSet<NguyenLieu> NguyenLieus { get; set; }
        public DbSet<DinhLuong> DinhLuongs { get; set; }
        public DbSet<NhaCungCap> NhaCungCaps { get; set; }
        public DbSet<PhieuNhapKho> PhieuNhapKhos { get; set; }
        public DbSet<ChiTietNhapKho> ChiTietNhapKhos { get; set; }
        public DbSet<PhieuKiemKho> PhieuKiemKhos { get; set; }
        public DbSet<ChiTietKiemKho> ChiTietKiemKhos { get; set; }
        public DbSet<PhieuXuatHuy> PhieuXuatHuys { get; set; }
        public DbSet<ChiTietXuatHuy> ChiTietXuatHuys { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<PhieuDatBan> PhieuDatBans { get; set; }
        public DbSet<KhuyenMai> KhuyenMais { get; set; }
        public DbSet<HoaDon_KhuyenMai> HoaDonKhuyenMais { get; set; }
        public DbSet<TheLoai> TheLoais { get; set; }
        public DbSet<TacGia> TacGias { get; set; }
        public DbSet<NhaXuatBan> NhaXuatBans { get; set; }
        public DbSet<Sach> Sachs { get; set; }
        public DbSet<PhieuThueSach> PhieuThueSachs { get; set; }
        public DbSet<ChiTietPhieuThue> ChiTietPhieuThues { get; set; }
        public DbSet<VaiTro> VaiTros { get; set; }
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<CaLamViec> CaLamViecs { get; set; }
        public DbSet<LichLamViec> LichLamViecs { get; set; }
        public DbSet<BangChamCong> BangChamCongs { get; set; }
        public DbSet<PhieuLuong> PhieuLuongs { get; set; }
        public DbSet<DonXinNghi> DonXinNghis { get; set; }
        public DbSet<CaiDat> CaiDats { get; set; }
        public DbSet<Quyen> Quyens { get; set; }
        public DbSet<VaiTro_Quyen> VaiTroQuyens { get; set; }
        public DbSet<GiaoDichThanhToan> GiaoDichThanhToans { get; set; }
        public DbSet<ChatLichSu> ChatLichSus { get; set; }
        public DbSet<DeXuatSanPham> DeXuatSanPhams { get; set; }
        public DbSet<DeXuatSach> DeXuatSachs { get; set; }
        public DbSet<ThongBao> ThongBaos { get; set; }
        public DbSet<DonViChuyenDoi> DonViChuyenDois { get; set; } // <-- THÊM DÒNG NÀY
        public DbSet<PhieuThuongPhat> PhieuThuongPhats { get; set; }
        // --- Cấu hình Khóa (Fluent API) ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseCollation("Vietnamese_CI_AS");
            // Cấu hình Khóa chính tổng hợp (Composite Primary Keys)
            modelBuilder.Entity<ChiTietPhuThuHoaDon>().HasKey(c => new { c.IdHoaDon, c.IdPhuThu });
            modelBuilder.Entity<DinhLuong>().HasKey(c => new { c.IdSanPham, c.IdNguyenLieu });
            modelBuilder.Entity<ChiTietNhapKho>().HasKey(c => new { c.IdPhieuNhapKho, c.IdNguyenLieu });
            modelBuilder.Entity<ChiTietKiemKho>().HasKey(c => new { c.IdPhieuKiemKho, c.IdNguyenLieu });
            modelBuilder.Entity<ChiTietXuatHuy>().HasKey(c => new { c.IdPhieuXuatHuy, c.IdNguyenLieu });
            modelBuilder.Entity<HoaDon_KhuyenMai>().HasKey(c => new { c.IdHoaDon, c.IdKhuyenMai });
            modelBuilder.Entity<ChiTietPhieuThue>().HasKey(c => new { c.IdPhieuThueSach, c.IdSach });
            modelBuilder.Entity<VaiTro_Quyen>().HasKey(c => new { c.IdVaiTro, c.IdQuyen });
            modelBuilder.Entity<DeXuatSanPham>().HasKey(c => new { c.IdSanPhamGoc, c.IdSanPhamDeXuat, c.LoaiDeXuat });
            modelBuilder.Entity<DeXuatSach>().HasKey(c => new { c.IdSachGoc, c.IdSachDeXuat, c.LoaiDeXuat });
            // === THÊM CẤU HÌNH MỚI NÀY (ĐỂ GIẢI QUYẾT XUNG ĐỘT FK) ===
            modelBuilder.Entity<PhieuThuongPhat>()
                .HasOne(p => p.NhanVien)
                .WithMany() // Không tạo collection
                .HasForeignKey(p => p.IdNhanVien)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PhieuThuongPhat>()
                .HasOne(p => p.NguoiTao)
                .WithMany() // Không tạo collection
                .HasForeignKey(p => p.IdNguoiTao)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PhieuThuongPhat>()
                .HasOne(p => p.PhieuLuong)
                .WithMany() // Không tạo collection
                .HasForeignKey(p => p.IdPhieuLuong)
                .OnDelete(DeleteBehavior.SetNull); // Nếu xóa phiếu lương, set null
            // --- THÊM 2 DÒNG NÀY ĐỂ SỬA WARNING ---
            modelBuilder.Entity<Sach>()
                .Property(s => s.GiaBia)
                .HasColumnType("decimal(18, 2)");
            // --- THÊM 2 DÒNG NÀY ĐỂ SỬA WARNING ---
            // <-- THÊM KHÓA NGOẠI MỚI CHO DINHLUONG (NẾU CHƯA CÓ) -->
            modelBuilder.Entity<DinhLuong>()
                .HasOne(d => d.DonViSuDung)
                .WithMany(dv => dv.DinhLuongs)
                .HasForeignKey(d => d.IdDonViSuDung)
                .OnDelete(DeleteBehavior.NoAction); // Rất quan trọng

            // Cấu hình Quan hệ đặc biệt (ví dụ: Tự tham chiếu)
            modelBuilder.Entity<DanhMuc>()
                .HasOne(d => d.DanhMucCha)
                .WithMany(d => d.DanhMucCons)
                .HasForeignKey(d => d.IdDanhMucCha)
                .OnDelete(DeleteBehavior.NoAction); // Tránh lỗi self-referencing cascade

            modelBuilder.Entity<DonXinNghi>()
                .HasOne(d => d.NguoiDuyet)
                .WithMany(nv => nv.DonXinNghiNguoiDuyets)
                .HasForeignKey(d => d.IdNguoiDuyet)
                .OnDelete(DeleteBehavior.NoAction);

            // Cấu hình Quan hệ cho DeXuatSanPham
            modelBuilder.Entity<DeXuatSanPham>()
                .HasOne(d => d.SanPhamGoc)
                .WithMany(p => p.DeXuatSanPhamGocs)
                .HasForeignKey(d => d.IdSanPhamGoc)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DeXuatSanPham>()
                .HasOne(d => d.SanPhamDeXuat)
                .WithMany(p => p.DeXuatSanPhamDeXuats)
                .HasForeignKey(d => d.IdSanPhamDeXuat)
                .OnDelete(DeleteBehavior.NoAction);

            // Cấu hình Quan hệ cho DeXuatSach
            modelBuilder.Entity<DeXuatSach>()
               .HasOne(d => d.SachGoc)
               .WithMany(p => p.DeXuatSachGocs)
               .HasForeignKey(d => d.IdSachGoc)
               .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DeXuatSach>()
                .HasOne(d => d.SachDeXuat)
                .WithMany(p => p.DeXuatSachDeXuats)
                .HasForeignKey(d => d.IdSachDeXuat)
                .OnDelete(DeleteBehavior.NoAction);

            // Tắt ON DELETE CASCADE cho các khóa ngoại gây lỗi (nếu cần)
            // Ví dụ: Bảng NhatKyHuyMon
            modelBuilder.Entity<NhatKyHuyMon>()
                .HasOne(n => n.HoaDon)
                .WithMany(h => h.NhatKyHuyMons)
                .HasForeignKey(n => n.IdHoaDon)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<NhatKyHuyMon>()
                .HasOne(n => n.NhanVienHuy)
                .WithMany(nv => nv.NhatKyHuyMons)
                .HasForeignKey(n => n.IdNhanVienHuy)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}