using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp
{
    public class DashboardSummaryDto
    {
        public decimal TongDoanhThuHomNay { get; set; }
        public int TongDonHangHomNay { get; set; }
        public string SanPhamBanChayHomNay { get; set; } = "Chưa có";
        public List<ChartDataPoint> DoanhThu30Ngay { get; set; } = new();
    }
}