using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/baocaonhansu")]
    [ApiController]
    public class BaoCaoNhanSuController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public BaoCaoNhanSuController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API Lấy các bộ lọc (Nhân viên, Vai trò)
        /// </summary>
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            var dto = new BaoCaoNhanSu_FiltersDto
            {
                // Lấy tất cả NV (cả nghỉ việc)
                NhanViens = await _context.NhanViens
                    .OrderBy(nv => nv.HoTen)
                    .Select(nv => new NhanVienLookupDto { IdNhanVien = nv.IdNhanVien, HoTen = nv.HoTen })
                    .ToListAsync(),

                VaiTros = await _context.VaiTros
                    .OrderBy(v => v.TenVaiTro)
                    .Select(v => new FilterLookupDto { Id = v.IdVaiTro, Ten = v.TenVaiTro })
                    .ToListAsync()
            };
            return Ok(dto);
        }

        /// <summary>
        /// API Tạo Báo Cáo Tổng Hợp (Đã sửa lỗi LINQ)
        /// </summary>
        [HttpPost("report")]
        public async Task<IActionResult> GetHrReport([FromBody] BaoCaoNhanSuRequestDto filters)
        {
            var report = new BaoCaoNhanSuDto();

            // 1. Lọc danh sách nhân viên cơ sở (base query)
            var nhanVienQuery = _context.NhanViens
                .Include(nv => nv.VaiTro)
                .AsQueryable();

            if (filters.IdNhanVien > 0)
                nhanVienQuery = nhanVienQuery.Where(nv => nv.IdNhanVien == filters.IdNhanVien);
            if (filters.IdVaiTro > 0)
                nhanVienQuery = nhanVienQuery.Where(nv => nv.IdVaiTro == filters.IdVaiTro);
            if (filters.TrangThaiNhanVien != "Tất cả")
                nhanVienQuery = nhanVienQuery.Where(nv => nv.TrangThaiLamViec == filters.TrangThaiNhanVien);

            var nhanVienIds = await nhanVienQuery.Select(nv => nv.IdNhanVien).ToListAsync();
            report.Kpi.SoLuongNhanVien = nhanVienIds.Count;

            // Lấy danh sách nhân viên (đã lọc) để join ở client
            var nhanVienDaLoc = await nhanVienQuery.ToDictionaryAsync(nv => nv.IdNhanVien);

            // 2. Lấy dữ liệu Phiếu Lương (Tab 1, Chart, và KPI Lương/Giờ)
            var phieuLuongQuery = _context.PhieuLuongs
                .Where(p => nhanVienIds.Contains(p.IdNhanVien) &&
                            p.NgayTao >= filters.StartDate &&
                            p.NgayTao <= filters.EndDate);

            report.Kpi.TongLuongDaTra = await phieuLuongQuery.SumAsync(p => p.ThucLanh);
            report.Kpi.TongGioLam = (double)await phieuLuongQuery.SumAsync(p => p.TongGioLam);

            // Dùng ToList() để thực thi truy vấn SQL
            var bangLuongData = await phieuLuongQuery
                .GroupBy(p => p.IdNhanVien) // Group theo ID
                .Select(g => new
                {
                    IdNhanVien = g.Key,
                    TongGioLam = (double)g.Sum(p => p.TongGioLam),
                    TongLuongCoBan = g.Sum(p => p.LuongCoBan * p.TongGioLam), // Tính tổng trước
                    TongThuong = g.Sum(p => p.TienThuong),
                    TongPhat = g.Sum(p => p.KhauTru),
                    ThucLanh = g.Sum(p => p.ThucLanh)
                })
                .ToListAsync(); // <-- THỰC THI TRUY VẤN TẠI ĐÂY

            // Join ở client-side
            report.BangLuongChiTiet = bangLuongData
                .Where(bl => nhanVienDaLoc.ContainsKey(bl.IdNhanVien)) // Đảm bảo nhân viên vẫn còn trong bộ lọc
                .Select(bl => new BaoCaoNhanSu_BangLuongDto
                {
                    IdNhanVien = bl.IdNhanVien,
                    HoTenNhanVien = nhanVienDaLoc[bl.IdNhanVien].HoTen,
                    TenVaiTro = nhanVienDaLoc[bl.IdNhanVien].VaiTro.TenVaiTro,
                    TongGioLam = bl.TongGioLam,
                    TongLuongCoBan = bl.TongLuongCoBan,
                    TongThuong = bl.TongThuong,
                    TongPhat = bl.TongPhat,
                    ThucLanh = bl.ThucLanh
                })
                .OrderByDescending(x => x.ThucLanh)
                .ToList();


            // === BẮT ĐẦU SỬA LỖI LINQ (Dòng 100) ===

            // 3. Lấy dữ liệu Nghỉ Phép (Tab 2 và KPI Nghỉ)
            var donNghiQuery = _context.DonXinNghis
                .Where(d => nhanVienIds.Contains(d.IdNhanVien) &&
                            d.TrangThai == "Đã duyệt" &&
                            d.NgayBatDau >= filters.StartDate &&
                            d.NgayKetThuc <= filters.EndDate);

            // Bước 1: Tải dữ liệu thô về RAM
            var donNghiData = await donNghiQuery
                .Select(d => new
                {
                    d.IdNhanVien,
                    d.NgayBatDau,
                    d.NgayKetThuc
                })
                .ToListAsync(); // <-- THỰC THI TRUY VẤN

            // Bước 2: Group và Tính toán ở client-side (trong RAM)
            var nghiPhepData = donNghiData
                .GroupBy(d => d.IdNhanVien)
                .Select(g => new
                {
                    IdNhanVien = g.Key,
                    SoDonDaDuyet = g.Count(),
                    // Tính tổng số ngày nghỉ (giờ đã ở client-side)
                    TongSoNgayNghi = g.Sum(d => (d.NgayKetThuc.Date - d.NgayBatDau.Date).Days + 1)
                })
                .ToList();

            // Bước 3: Join với thông tin nhân viên
            report.ThongKeNghiPhep = nghiPhepData
                .Where(np => nhanVienDaLoc.ContainsKey(np.IdNhanVien)) // Đảm bảo nhân viên vẫn còn trong bộ lọc
                .Select(x => new BaoCaoNhanSu_NghiPhepDto
                {
                    IdNhanVien = x.IdNhanVien,
                    HoTenNhanVien = nhanVienDaLoc[x.IdNhanVien].HoTen,
                    TenVaiTro = nhanVienDaLoc[x.IdNhanVien].VaiTro.TenVaiTro,
                    SoDonDaDuyet = x.SoDonDaDuyet,
                    TongSoNgayNghi = x.TongSoNgayNghi
                })
                .OrderByDescending(x => x.TongSoNgayNghi)
                .ToList();

            // === KẾT THÚC SỬA LỖI LINQ ===

            report.Kpi.TongSoNgayNghi = report.ThongKeNghiPhep.Sum(x => x.TongSoNgayNghi);

            // 4. Lấy dữ liệu Biểu đồ (Tổng lương trả theo ngày)
            report.LuongChartData = await phieuLuongQuery
                .GroupBy(p => p.NgayTao.Date)
                .Select(g => new ChartDataPoint
                {
                    Ngay = g.Key,
                    TongTien = g.Sum(p => p.ThucLanh)
                })
                .OrderBy(c => c.Ngay)
                .ToListAsync();

            return Ok(report);
        }
    }

    // SQL Server 2017 trở xuống không có DATEDIFF_BIG, EF Core cần trợ giúp
    public static class DbFunctions
    {
        [DbFunction(Name = "DATEDIFF", IsBuiltIn = true)]
        public static int DateDiffDay(DateTime startDate, DateTime endDate)
        {
            throw new NotSupportedException();
        }
    }
}