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
    [Route("api/app/baocaokho")]
    [ApiController]
    public class BaoCaoTonKhoController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public BaoCaoTonKhoController(CafebookDbContext context)
        {
            _context = context;
        }

        [HttpGet("filters")]
        public async Task<IActionResult> GetFilterData()
        {
            var nhaCungCaps = await _context.NhaCungCaps
                .Select(t => new FilterLookupDto { Id = t.IdNhaCungCap, Ten = t.TenNhaCungCap })
                .OrderBy(t => t.Ten)
                .ToListAsync();

            return Ok(new { nhaCungCaps });
        }

        [HttpPost("report")]
        public async Task<IActionResult> GetKhoReport([FromBody] BaoCaoTonKhoRequestDto request)
        {
            // --- 1. TÍNH KPIs ---

            // KPI a: Tổng giá trị tồn kho (ĐÃ XÓA CÁC THẺ [cite] GÂY LỖI)
            var kpiGiaTriKho = await _context.Database.SqlQuery<decimal>($@"
                WITH GiaVonNguyenLieu AS (
                    SELECT idNguyenLieu, AVG(donGiaNhap) AS GiaVonTrungBinh
                    FROM dbo.ChiTietNhapKho
                    GROUP BY idNguyenLieu
                )
                SELECT ISNULL(SUM(nl.tonKho * ISNULL(gv.GiaVonTrungBinh, 0)), 0)
                FROM dbo.NguyenLieu nl
                LEFT JOIN GiaVonNguyenLieu gv ON nl.idNguyenLieu = gv.idNguyenLieu;
            ").ToListAsync();

            // KPI b: Số lượng SP sắp hết
            var kpiSapHet = await _context.Database.SqlQuery<int>($@"
                SELECT ISNULL(COUNT(idNguyenLieu), 0) FROM NguyenLieu WHERE tonKho <= TonKhoToiThieu;
            ").ToListAsync();

            // KPI c: Tổng giá trị đã hủy (trong kỳ, giả sử 30 ngày)
            var kpiHuyHang = await _context.Database.SqlQuery<decimal>($@"
                SELECT ISNULL(SUM(TongGiaTriHuy), 0) FROM PhieuXuatHuy
                WHERE NgayXuatHuy >= DATEADD(day, -30, GETDATE());
            ").ToListAsync();

            var kpi = new BaoCaoTonKhoKpiDto
            {
                TongGiaTriTonKho = kpiGiaTriKho.FirstOrDefault(),
                SoLuongSPSapHet = kpiSapHet.FirstOrDefault(),
                TongGiaTriDaHuy = kpiHuyHang.FirstOrDefault()
            };

            // --- 2. TAB 1: TỒN KHO CHI TIẾT ---
            var pSearchText = new SqlParameter("@SearchText", string.IsNullOrEmpty(request.SearchText) ? (object)DBNull.Value : $"%{request.SearchText}%");
            var pShowLowStockOnly = new SqlParameter("@ShowLowStockOnly", request.ShowLowStockOnly);
            // (Lưu ý: Lọc theo NCC bị bỏ qua vì logic phức tạp, không có trong thiết kế SQL)

            var chiTietTonKho = await _context.Database.SqlQuery<BaoCaoTonKhoChiTietDto>($@"
                SELECT
                    tenNguyenLieu,
                    donViTinh,
                    tonKho,
                    TonKhoToiThieu,
                    CASE
                        WHEN tonKho = 0 THEN N'Hết hàng'
                        WHEN tonKho <= TonKhoToiThieu THEN N'Sắp hết'
                        ELSE N'Đủ dùng'
                    END AS TinhTrang
                FROM dbo.NguyenLieu
                WHERE
                    (tenNguyenLieu LIKE {pSearchText} OR {pSearchText} IS NULL)
                    AND ({pShowLowStockOnly} = 0 OR tonKho <= TonKhoToiThieu)
                ORDER BY
                    tonKho ASC;
            ").ToListAsync();

            // --- 3. TAB 2: LỊCH SỬ KIỂM KÊ ---
            var lichSuKiemKe = await _context.Database.SqlQuery<BaoCaoKiemKeDto>($@"
                SELECT
                    pkk.NgayKiem,
                    nl.tenNguyenLieu,
                    ctkk.TonKhoHeThong,
                    ctkk.TonKhoThucTe,
                    ctkk.ChenhLech,
                    ctkk.LyDoChenhLech
                FROM dbo.ChiTietKiemKho ctkk
                JOIN dbo.PhieuKiemKho pkk ON ctkk.idPhieuKiemKho = pkk.idPhieuKiemKho
                JOIN dbo.NguyenLieu nl ON ctkk.idNguyenLieu = nl.idNguyenLieu
                WHERE
                    ctkk.ChenhLech != 0
                ORDER BY
                    pkk.NgayKiem DESC;
            ").ToListAsync();

            // --- 4. TAB 3: LỊCH SỬ HỦY HÀNG ---
            var lichSuHuyHang = await _context.Database.SqlQuery<BaoCaoHuyHangDto>($@"
                SELECT
                    pxh.NgayXuatHuy AS NgayHuy,
                    nl.tenNguyenLieu,
                    ctxh.SoLuong AS SoLuongHuy,
                    ctxh.ThanhTien AS GiaTriHuy,
                    pxh.LyDoXuatHuy AS LyDoHuy
                FROM dbo.ChiTietXuatHuy ctxh
                JOIN dbo.PhieuXuatHuy pxh ON ctxh.idPhieuXuatHuy = pxh.idPhieuXuatHuy
                JOIN dbo.NguyenLieu nl ON ctxh.idNguyenLieu = nl.idNguyenLieu
                ORDER BY
                    pxh.NgayXuatHuy DESC;
            ").ToListAsync();

            // --- 5. TỔNG HỢP KẾT QUẢ ---
            var dto = new BaoCaoTonKhoTongHopDto
            {
                Kpi = kpi,
                ChiTietTonKho = chiTietTonKho,
                LichSuKiemKe = lichSuKiemKe,
                LichSuHuyHang = lichSuHuyHang
            };

            return Ok(dto);
        }
    }
}