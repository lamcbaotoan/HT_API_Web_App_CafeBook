using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/baocao")]
    [ApiController]
    public class BaoCaoController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public BaoCaoController(CafebookDbContext context)
        {
            _context = context;
        }

        [HttpPost("doanhthu")]
        public async Task<IActionResult> GetDoanhThuReport([FromBody] BaoCaoRequestDto request)
        {
            var startDate = request.StartDate.Date;
            var endDate = request.EndDate.Date.AddDays(1);

            // 1. TÍNH DOANH THU (Code này đã đúng)
            var chiTietDoanhThu = (await _context.Database.SqlQuery<BaoCaoChiTietDoanhThuDto>($@"
                    SELECT
                        ISNULL(SUM(tongTienGoc), 0) AS TongDoanhThuGoc,
                        ISNULL(SUM(giamGia), 0) AS TongGiamGia,
                        ISNULL(SUM(TongPhuThu), 0) AS TongPhuThu,
                        ISNULL(SUM(thanhTien), 0) AS DoanhThuRong,
                        ISNULL(COUNT(idHoaDon), 0) AS SoLuongHoaDon,
                        ISNULL(AVG(thanhTien), 0) AS GiaTriTrungBinhHD
                    FROM dbo.HoaDon
                    WHERE trangThai = N'Đã thanh toán'
                    AND thoiGianThanhToan >= {startDate} AND thoiGianThanhToan < {endDate};
                ").ToListAsync()).FirstOrDefault() ?? new BaoCaoChiTietDoanhThuDto();

            // 2. TÍNH GIÁ VỐN (COGS) (Sửa lỗi CS1503 tại đây)
            // Xóa 'var cogsSql = ...' và truyền chuỗi nội suy TRỰC TIẾP
            var cogsResult = await _context.Database.SqlQuery<decimal>($@"
                WITH GiaVonNguyenLieu AS (
                    SELECT idNguyenLieu, AVG(donGiaNhap) AS GiaVonTrungBinh
                    FROM dbo.ChiTietNhapKho GROUP BY idNguyenLieu
                ),
                SanPhamDaBan AS (
                    SELECT cthd.idSanPham, SUM(cthd.soLuong) AS TongSoLuongBan
                    FROM dbo.ChiTietHoaDon cthd
                    JOIN dbo.HoaDon hd ON cthd.idHoaDon = hd.idHoaDon
                    WHERE hd.trangThai = N'Đã thanh toán'
                    AND hd.thoiGianThanhToan >= {startDate} AND hd.thoiGianThanhToan < {endDate}
                    GROUP BY cthd.idSanPham
                )
                SELECT ISNULL(SUM(spb.TongSoLuongBan * dl.SoLuongSuDung * gv.GiaVonTrungBinh), 0)
                FROM SanPhamDaBan spb
                JOIN dbo.DinhLuong dl ON spb.idSanPham = dl.idSanPham
                JOIN GiaVonNguyenLieu gv ON dl.idNguyenLieu = gv.idNguyenLieu;
            ").ToListAsync(); // Lỗi CS1503 đã được sửa

            decimal tongGiaVon_COGS = cogsResult.FirstOrDefault();

            // 3. TÍNH CHI PHÍ VẬN HÀNH (OPEX) (Code này đã đúng)
            var opexResult = (await _context.Database.SqlQuery<OpexDto>($@"
                    SELECT 
                    ISNULL((SELECT SUM(thucLanh) 
                        FROM dbo.PhieuLuong
                        WHERE trangThai = N'Đã thanh toán'
                        AND ngayTao >= {startDate} AND ngayTao < {endDate}
                    ), 0) AS TongChiPhiLuong,
                    
                    ISNULL((SELECT SUM(TongGiaTriHuy) 
                        FROM dbo.PhieuXuatHuy
                        WHERE NgayXuatHuy >= {startDate} AND NgayXuatHuy < {endDate}
                    ), 0) AS TongChiPhiHuyHang;
                ").ToListAsync()).FirstOrDefault() ?? new OpexDto();

            // 4. TOP SẢN PHẨM (Code này đã đúng)
            var topSanPham = await _context.Database.SqlQuery<TopSanPhamDto>($@"
                    SELECT TOP 10
                        sp.tenSanPham,
                        SUM(cthd.soLuong) AS TongSoLuongBan,
                        SUM(cthd.thanhTien) AS TongDoanhThu
                    FROM dbo.ChiTietHoaDon cthd
                    JOIN dbo.SanPham sp ON cthd.idSanPham = sp.idSanPham
                    JOIN dbo.HoaDon hd ON cthd.idHoaDon = hd.idHoaDon
                    WHERE hd.trangThai = N'Đã thanh toán'
                    AND hd.thoiGianThanhToan >= {startDate} AND hd.thoiGianThanhToan < {endDate}
                    GROUP BY sp.tenSanPham
                    ORDER BY TongSoLuongBan DESC;
                ").ToListAsync();

            // 5. TỔNG HỢP KẾT QUẢ (Code này đã đúng)
            var dto = new BaoCaoTongHopDto
            {
                ChiTietDoanhThu = chiTietDoanhThu,
                ChiTietChiPhi = new BaoCaoChiPhiDto
                {
                    TongGiaVon_COGS = tongGiaVon_COGS,
                    TongChiPhiLuong = opexResult.TongChiPhiLuong,
                    TongChiPhiHuyHang = opexResult.TongChiPhiHuyHang
                },
                TopSanPham = topSanPham,
                Kpi = new BaoCaoKpiDto() // Tính toán KPI
            };

            // Tính toán KPI cuối cùng
            dto.Kpi.DoanhThuRong = dto.ChiTietDoanhThu.DoanhThuRong;
            dto.Kpi.TongGiaVon = dto.ChiTietChiPhi.TongGiaVon_COGS;
            dto.Kpi.LoiNhuanGop = dto.Kpi.DoanhThuRong - dto.Kpi.TongGiaVon;
            dto.Kpi.ChiPhiOpex = dto.ChiTietChiPhi.TongChiPhiLuong + dto.ChiTietChiPhi.TongChiPhiHuyHang;
            dto.Kpi.LoiNhuanRong = dto.Kpi.LoiNhuanGop - dto.Kpi.ChiPhiOpex;

            return Ok(dto);
        }
    }
}