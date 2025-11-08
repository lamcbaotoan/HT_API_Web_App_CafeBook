// Tập tin: CafebookApi/Controllers/App/BaoCaoSachController.cs
using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic; // Thêm

namespace CafebookApi.Controllers.App
{
    [Route("api/app/baocaosach")]
    [ApiController]
    public class BaoCaoSachController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public BaoCaoSachController(CafebookDbContext context)
        {
            _context = context;
        }

        [HttpGet("filters")]
        public async Task<IActionResult> GetFilterData()
        {
            var theLoais = await _context.TheLoais
                .Select(t => new FilterLookupDto { Id = t.IdTheLoai, Ten = t.TenTheLoai, MoTa = t.MoTa })
                .OrderBy(t => t.Ten)
                .ToListAsync();

            var tacGias = await _context.TacGias
                .Select(t => new FilterLookupDto { Id = t.IdTacGia, Ten = t.TenTacGia, MoTa = t.GioiThieu })
                .OrderBy(t => t.Ten)
                .ToListAsync();

            return Ok(new { theLoais, tacGias });
        }

        [HttpPost("report")]
        public async Task<IActionResult> GetSachReport([FromBody] BaoCaoSachRequestDto request)
        {
            // SỬA LỖI: Chuyển sang biến đơn giản để EF Core tự tham số hóa
            string? pSearchText = string.IsNullOrEmpty(request.SearchText) ? null : $"%{request.SearchText}%";
            int? pTheLoaiId = request.TheLoaiId == 0 ? null : request.TheLoaiId;
            int? pTacGiaId = request.TacGiaId == 0 ? null : request.TacGiaId;

            // 1. TÍNH KPIs (Không đổi)
            var kpi = (await _context.Database.SqlQuery<BaoCaoSachKpiDto>($@"
                SELECT
                    ISNULL(COUNT(DISTINCT idSach), 0) AS TongDauSach,
                    ISNULL(SUM(soLuongTong), 0) AS TongSoLuong,
                    (SELECT ISNULL(COUNT(idSach), 0) FROM dbo.ChiTietPhieuThue WHERE ngayTraThucTe IS NULL) AS DangChoThue,
                    (ISNULL(SUM(soLuongTong), 0) - (SELECT ISNULL(COUNT(idSach), 0) FROM dbo.ChiTietPhieuThue WHERE ngayTraThucTe IS NULL)) AS SanSang
                FROM dbo.Sach;
            ").ToListAsync()).FirstOrDefault() ?? new BaoCaoSachKpiDto();

            // 2. TAB 1: TỒN KHO CHI TIẾT (VIẾT LẠI HOÀN TOÀN)
            var chiTietTonKho = await _context.Database.SqlQuery<BaoCaoSachChiTietDto>($@"
                WITH SachDangThue AS (
                    SELECT idSach, COUNT(idSach) AS SoLuongDangMuon
                    FROM dbo.ChiTietPhieuThue
                    WHERE ngayTraThucTe IS NULL
                    GROUP BY idSach
                ),
                SachTacGiasAgg AS (
                    SELECT
                        stg.idSach,
                        STRING_AGG(tg.tenTacGia, ', ') AS tenTacGia
                    FROM dbo.Sach_TacGia stg
                    JOIN dbo.TacGia tg ON stg.idTacGia = tg.idTacGia
                    GROUP BY stg.idSach
                ),
                SachTheLoaisAgg AS (
                    SELECT
                        stl.idSach,
                        STRING_AGG(tl.tenTheLoai, ', ') AS tenTheLoai
                    FROM dbo.Sach_TheLoai stl
                    JOIN dbo.TheLoai tl ON stl.idTheLoai = tl.idTheLoai
                    GROUP BY stl.idSach
                )
                SELECT
                    s.tenSach,
                    ISNULL(stg_agg.tenTacGia, 'N/A') AS tenTacGia,
                    ISNULL(stl_agg.tenTheLoai, 'N/A') AS tenTheLoai,
                    s.soLuongTong,
                    ISNULL(sdt.SoLuongDangMuon, 0) AS SoLuongDangMuon,
                    (s.soLuongTong - ISNULL(sdt.SoLuongDangMuon, 0)) AS SoLuongConLai
                FROM dbo.Sach s
                LEFT JOIN SachTheLoaisAgg stl_agg ON s.idSach = stl_agg.idSach
                LEFT JOIN SachTacGiasAgg stg_agg ON s.idSach = stg_agg.idSach
                LEFT JOIN SachDangThue sdt ON s.idSach = sdt.idSach
                WHERE
                    (s.tenSach LIKE {pSearchText} OR stg_agg.tenTacGia LIKE {pSearchText} OR {pSearchText} IS NULL)
                    AND (EXISTS(SELECT 1 FROM dbo.Sach_TheLoai stl WHERE stl.idSach = s.idSach AND stl.idTheLoai = {pTheLoaiId}) OR {pTheLoaiId} IS NULL)
                    AND (EXISTS(SELECT 1 FROM dbo.Sach_TacGia stg WHERE stg.idSach = s.idSach AND stg.idTacGia = {pTacGiaId}) OR {pTacGiaId} IS NULL)
                ORDER BY SoLuongConLai ASC, s.tenSach;
            ").ToListAsync();

            // 3. TAB 2: SÁCH TRỄ HẠN (Không đổi)
            var sachTreHan = await _context.Database.SqlQuery<BaoCaoSachTreHanDto>($@"
                SELECT
                    s.tenSach,
                    kh.hoTen,
                    kh.soDienThoai,
                    pts.ngayThue,
                    ctpt.ngayHenTra,
                    CASE
                        WHEN ctpt.ngayHenTra < GETDATE() 
                        THEN N'Trễ ' + CAST(DATEDIFF(DAY, ctpt.ngayHenTra, GETDATE()) AS NVARCHAR) + N' ngày'
                        ELSE N'Đang thuê'
                    END AS TinhTrang
                FROM dbo.ChiTietPhieuThue ctpt
                JOIN dbo.PhieuThueSach pts ON ctpt.idPhieuThueSach = pts.idPhieuThueSach
                JOIN dbo.Sach s ON ctpt.idSach = s.idSach
                JOIN dbo.KhachHang kh ON pts.idKhachHang = kh.idKhachHang
                WHERE ctpt.ngayTraThucTe IS NULL
                ORDER BY ctpt.ngayHenTra ASC;
            ").ToListAsync();

            // 4. TAB 3: TOP SÁCH THUÊ (VIẾT LẠI)
            var topSachThue = await _context.Database.SqlQuery<TopSachDuocThueDto>($@"
                WITH SachTacGiasAgg AS (
                    SELECT
                        stg.idSach,
                        STRING_AGG(tg.tenTacGia, ', ') AS tenTacGia
                    FROM dbo.Sach_TacGia stg
                    JOIN dbo.TacGia tg ON stg.idTacGia = tg.idTacGia
                    GROUP BY stg.idSach
                )
                SELECT TOP 10
                    s.tenSach,
                    ISNULL(stg_agg.tenTacGia, 'N/A') AS tenTacGia,
                    COUNT(ctpt.idSach) AS TongLuotThue
                FROM dbo.ChiTietPhieuThue ctpt
                JOIN dbo.Sach s ON ctpt.idSach = s.idSach
                LEFT JOIN SachTacGiasAgg stg_agg ON s.idSach = stg_agg.idSach
                GROUP BY s.tenSach, stg_agg.tenTacGia
                ORDER BY TongLuotThue DESC;
            ").ToListAsync();

            // 5. TỔNG HỢP KẾT QUẢ (Không đổi)
            var dto = new BaoCaoSachTongHopDto
            {
                Kpi = kpi,
                ChiTietTonKho = chiTietTonKho,
                SachTreHan = sachTreHan,
                TopSachThue = topSachThue
            };

            return Ok(dto);
        }
    }
}