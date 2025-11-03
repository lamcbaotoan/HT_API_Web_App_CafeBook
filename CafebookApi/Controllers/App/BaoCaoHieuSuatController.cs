using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/baocaohieusuat")]
    [ApiController]
    public class BaoCaoHieuSuatController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public BaoCaoHieuSuatController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API để lấy dữ liệu cho ComboBox Vai Trò
        /// </summary>
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilterData()
        {
            var vaiTros = await _context.VaiTros
                .Select(t => new FilterLookupDto { Id = t.IdVaiTro, Ten = t.TenVaiTro })
                .OrderBy(t => t.Ten)
                .ToListAsync();

            return Ok(new { vaiTros });
        }

        /// <summary>
        /// API tạo báo cáo chính
        /// </summary>
        [HttpPost("report")]
        public async Task<IActionResult> GetHieuSuatReport([FromBody] BaoCaoHieuSuatRequestDto request)
        {
            var startDate = request.StartDate.Date;
            var endDate = request.EndDate.Date.AddDays(1);

            // Tham số hóa
            object pSearchTextValue = string.IsNullOrEmpty(request.SearchText) ? DBNull.Value : $"%{request.SearchText}%";
            var pStartDate = new SqlParameter("@StartDate", startDate);
            var pEndDate = new SqlParameter("@EndDate", endDate);
            var pVaiTroId = new SqlParameter("@VaiTroId", (object)request.VaiTroId ?? DBNull.Value);
            var pSearchText = new SqlParameter("@SearchText", string.IsNullOrEmpty(request.SearchText) ? (object)DBNull.Value : $"%{request.SearchText}%");

            // 1. TÍNH KPIs
            var kpi = (await _context.Database.SqlQuery<BaoCaoHieuSuatKpiDto>($@"
                SELECT
                    ISNULL(SUM(hd.thanhTien), 0) AS TongDoanhThu,
                    ISNULL(SUM(bc.soGioLam), 0) AS TongGioLam,
                    ISNULL(COUNT(bc.idChamCong), 0) AS TongSoCaLam,
                    ISNULL(COUNT(nkhm.idNhatKy), 0) AS TongLanHuyMon
                FROM dbo.NhanVien nv
                LEFT JOIN dbo.HoaDon hd ON nv.idNhanVien = hd.idNhanVien 
                    AND hd.trangThai = N'Đã thanh toán' AND hd.thoiGianThanhToan >= {pStartDate} AND hd.thoiGianThanhToan < {pEndDate}
                LEFT JOIN dbo.LichLamViec llv ON nv.idNhanVien = llv.idNhanVien 
                    AND llv.ngayLam >= {pStartDate} AND llv.ngayLam < {pEndDate}
                LEFT JOIN dbo.BangChamCong bc ON llv.idLichLamViec = bc.idLichLamViec
                LEFT JOIN dbo.NhatKyHuyMon nkhm ON nv.idNhanVien = nkhm.idNhanVienHuy 
                    AND nkhm.ThoiGianHuy >= {pStartDate} AND nkhm.ThoiGianHuy < {pEndDate}
                WHERE (nv.hoTen LIKE {pSearchText} OR {pSearchText} IS NULL)
                  AND (nv.idVaiTro = {pVaiTroId} OR {pVaiTroId} IS NULL);
            ").ToListAsync()).FirstOrDefault() ?? new BaoCaoHieuSuatKpiDto();
            
            // 2. TAB 1: HIỆU SUẤT BÁN HÀNG
            var salesPerformance = await _context.Database.SqlQuery<BaoCaoSalesDto>($@"
                SELECT
                    nv.hoTen,
                    vt.tenVaiTro,
                    ISNULL(SUM(hd.thanhTien), 0) AS TongDoanhThu,
                    ISNULL(COUNT(DISTINCT hd.idHoaDon), 0) AS SoHoaDon,
                    ISNULL(AVG(hd.thanhTien), 0) AS DoanhThuTrungBinh,
                    (SELECT COUNT(nkhm.idNhatKy) 
                     FROM dbo.NhatKyHuyMon nkhm 
                     WHERE nkhm.idNhanVienHuy = nv.idNhanVien 
                     AND nkhm.ThoiGianHuy >= {pStartDate} AND nkhm.ThoiGianHuy < {pEndDate}) AS SoLanHuyMon
                FROM dbo.NhanVien nv
                JOIN dbo.VaiTro vt ON nv.idVaiTro = vt.idVaiTro
                LEFT JOIN dbo.HoaDon hd ON nv.idNhanVien = hd.idNhanVien 
                    AND hd.trangThai = N'Đã thanh toán' 
                    AND hd.thoiGianThanhToan >= {pStartDate} AND hd.thoiGianThanhToan < {pEndDate}
                WHERE
                    (vt.tenVaiTro IN (N'Thu ngân', N'Phục vụ', N'Quản lý', N'Quản trị viên'))
                    AND (nv.hoTen LIKE {pSearchText} OR {pSearchText} IS NULL)
                    AND (nv.idVaiTro = {pVaiTroId} OR {pVaiTroId} IS NULL)
                GROUP BY nv.idNhanVien, nv.hoTen, vt.tenVaiTro
                ORDER BY TongDoanhThu DESC;
            ").ToListAsync();
            
            // 3. TAB 2: HIỆU SUẤT VẬN HÀNH
            var operationalPerformance = await _context.Database.SqlQuery<BaoCaoOperationsDto>($@"
                SELECT
                    nv.hoTen,
                    vt.tenVaiTro,
                    (SELECT COUNT(idPhieuNhapKho) FROM PhieuNhapKho WHERE idNhanVien = nv.idNhanVien AND ngayNhap >= {pStartDate} AND ngayNhap < {pEndDate}) AS PhieuNhap,
                    (SELECT COUNT(idPhieuKiemKho) FROM PhieuKiemKho WHERE idNhanVienKiem = nv.idNhanVien AND NgayKiem >= {pStartDate} AND NgayKiem < {pEndDate}) AS PhieuKiem,
                    (SELECT COUNT(idPhieuXuatHuy) FROM PhieuXuatHuy WHERE idNhanVienXuat = nv.idNhanVien AND NgayXuatHuy >= {pStartDate} AND NgayXuatHuy < {pEndDate}) AS PhieuHuy,
                    (SELECT COUNT(idDonXinNghi) FROM DonXinNghi WHERE idNguoiDuyet = nv.idNhanVien AND NgayDuyet >= {pStartDate} AND NgayDuyet < {pEndDate}) AS DonDuyet
                FROM dbo.NhanVien nv
                JOIN dbo.VaiTro vt ON nv.idVaiTro = vt.idVaiTro
                WHERE
                    (vt.tenVaiTro IN (N'Quản lý', N'Quản trị viên', N'Pha chế'))
                    AND (nv.hoTen LIKE {pSearchText} OR {pSearchText} IS NULL)
                    AND (nv.idVaiTro = {pVaiTroId} OR {pVaiTroId} IS NULL)
                GROUP BY nv.idNhanVien, nv.hoTen, vt.tenVaiTro;
            ").ToListAsync();

            // 4. TAB 3: CHUYÊN CẦN
            var attendance = await _context.Database.SqlQuery<BaoCaoAttendanceDto>($@"
                SELECT
                    nv.hoTen,
                    ISNULL(COUNT(llv.idLichLamViec), 0) AS TongSoCaLam,
                    ISNULL(SUM(bc.soGioLam), 0) AS TongGioLam,
                    (SELECT COUNT(idDonXinNghi) FROM DonXinNghi WHERE idNhanVien = nv.idNhanVien AND NgayBatDau >= {pStartDate} AND NgayKetThuc < {pEndDate}) AS SoDonXinNghi,
                    (SELECT COUNT(idDonXinNghi) FROM DonXinNghi WHERE idNhanVien = nv.idNhanVien AND TrangThai = N'Đã duyệt' AND NgayBatDau >= {pStartDate} AND NgayKetThuc < {pEndDate}) AS SoDonDaDuyet,
                    (SELECT COUNT(idDonXinNghi) FROM DonXinNghi WHERE idNhanVien = nv.idNhanVien AND TrangThai = N'Chờ duyệt' AND NgayBatDau >= {pStartDate} AND NgayKetThuc < {pEndDate}) AS SoDonChoDuyet
                FROM dbo.NhanVien nv
                LEFT JOIN dbo.LichLamViec llv ON nv.idNhanVien = llv.idNhanVien
                    AND llv.ngayLam >= {pStartDate} AND llv.ngayLam < {pEndDate}
                LEFT JOIN dbo.BangChamCong bc ON llv.idLichLamViec = bc.idLichLamViec
                WHERE (nv.hoTen LIKE {pSearchText} OR {pSearchText} IS NULL)
                  AND (nv.idVaiTro = {pVaiTroId} OR {pVaiTroId} IS NULL)
                GROUP BY nv.idNhanVien, nv.hoTen;
            ").ToListAsync();

            // 5. TỔNG HỢP KẾT QUẢ
            var dto = new BaoCaoHieuSuatTongHopDto
            {
                Kpi = kpi,
                SalesPerformance = salesPerformance,
                OperationalPerformance = operationalPerformance,
                Attendance = attendance
            };

            return Ok(dto);
        }
    }
}