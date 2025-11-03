using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System; // Thêm

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
                .Select(t => new FilterLookupDto { Id = t.IdTheLoai, Ten = t.TenTheLoai })
                .OrderBy(t => t.Ten)
                .ToListAsync();

            var tacGias = await _context.TacGias
                .Select(t => new FilterLookupDto { Id = t.IdTacGia, Ten = t.TenTacGia })
                .OrderBy(t => t.Ten)
                .ToListAsync();

            return Ok(new { theLoais, tacGias });
        }

        [HttpPost("report")]
        public async Task<IActionResult> GetSachReport([FromBody] BaoCaoSachRequestDto request)
        {
            // --- SỬA LỖI CS8600 TẠI ĐÂY ---
            // Khởi tạo an toàn hơn
            var pSearchText = new SqlParameter("@SearchText", (object)DBNull.Value);
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                pSearchText.Value = $"%{request.SearchText}%";
            }

            var pTheLoaiId = new SqlParameter("@TheLoaiId", (object)request.TheLoaiId ?? DBNull.Value);
            var pTacGiaId = new SqlParameter("@TacGiaId", (object)request.TacGiaId ?? DBNull.Value);
            // --- KẾT THÚC SỬA LỖI ---

            // 1. TÍNH KPIs (Code này OK)
            var kpi = (await _context.Database.SqlQuery<BaoCaoSachKpiDto>($@"
                SELECT
                    ISNULL(COUNT(DISTINCT idSach), 0) AS TongDauSach,
                    ISNULL(SUM(soLuongTong), 0) AS TongSoLuong,
                    (SELECT ISNULL(COUNT(idSach), 0) FROM dbo.ChiTietPhieuThue WHERE ngayTraThucTe IS NULL) AS DangChoThue,
                    (ISNULL(SUM(soLuongTong), 0) - (SELECT ISNULL(COUNT(idSach), 0) FROM dbo.ChiTietPhieuThue WHERE ngayTraThucTe IS NULL)) AS SanSang
                FROM dbo.Sach;
            ").ToListAsync()).FirstOrDefault() ?? new BaoCaoSachKpiDto();

            // 2. TAB 1: TỒN KHO CHI TIẾT
            var chiTietTonKho = await _context.Database.SqlQuery<BaoCaoSachChiTietDto>($@"
                WITH SachDangThue AS (
                    SELECT idSach, COUNT(idSach) AS SoLuongDangMuon
                    FROM dbo.ChiTietPhieuThue
                    WHERE ngayTraThucTe IS NULL
                    GROUP BY idSach
                )
                SELECT
                    s.tenSach,
                    tg.tenTacGia,
                    tl.tenTheLoai,
                    s.soLuongTong,
                    ISNULL(sdt.SoLuongDangMuon, 0) AS SoLuongDangMuon,
                    (s.soLuongTong - ISNULL(sdt.SoLuongDangMuon, 0)) AS SoLuongConLai
                FROM dbo.Sach s
                LEFT JOIN dbo.TheLoai tl ON s.idTheLoai = tl.idTheLoai
                LEFT JOIN dbo.TacGia tg ON s.idTacGia = tg.idTacGia
                LEFT JOIN SachDangThue sdt ON s.idSach = sdt.idSach
                WHERE
                    (s.tenSach LIKE {pSearchText} OR {pSearchText} IS NULL)
                    AND (s.idTheLoai = {pTheLoaiId} OR {pTheLoaiId} IS NULL)
                    AND (s.idTacGia = {pTacGiaId} OR {pTacGiaId} IS NULL)
                ORDER BY SoLuongConLai ASC, s.tenSach;
            ").ToListAsync();

            // 3. TAB 2: SÁCH TRỄ HẠN (Code này OK)
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

            // 4. TAB 3: TOP SÁCH THUÊ (Code này OK)
            var topSachThue = await _context.Database.SqlQuery<TopSachDuocThueDto>($@"
                SELECT TOP 10
                    s.tenSach,
                    tg.tenTacGia,
                    COUNT(ctpt.idSach) AS TongLuotThue
                FROM dbo.ChiTietPhieuThue ctpt
                JOIN dbo.Sach s ON ctpt.idSach = s.idSach
                LEFT JOIN dbo.TacGia tg ON s.idTacGia = tg.idTacGia
                GROUP BY s.tenSach, tg.tenTacGia
                ORDER BY TongLuotThue DESC;
            ").ToListAsync();

            // 5. TỔNG HỢP KẾT QUẢ (Code này OK)
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