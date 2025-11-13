// Tệp: AppCafebookApi/View/common/PhieuGiaoHangPreviewWindow.xaml.cs
using CafebookModel.Model.ModelApp.NhanVien;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Cần cho VisualTreeHelper

// === SỬA LỖI CS0246: Đổi 'common' (thường) thành 'Common' (hoa) ===
namespace AppCafebookApi.View.Common
{
    public partial class PhieuGiaoHangPreviewWindow : Window
    {
        // SỬA: Dùng DTO của Phiếu Giao Hàng (từ GoiMonDto)
        private readonly PhieuGoiMonPrintDto _data;

        public PhieuGiaoHangPreviewWindow(PhieuGoiMonPrintDto data)
        {
            InitializeComponent(); // Lỗi CS0103 đã được fix
            _data = data;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Bind Thông tin quán
            lblTenQuan.Text = _data.TenQuan;
            lblDiaChi.Text = _data.DiaChiQuan;
            lblSoDienThoai.Text = $"SĐT: {_data.SdtQuan}";

            // 2. Bind Thông tin Hóa đơn
            lblTieuDe.Text = "PHIẾU GIAO HÀNG";
            lblSoHoaDon.Text = $"Số HĐ: {_data.IdPhieu}";
            lblBan.Text = $"Loại: {_data.SoBan}"; // "Giao hàng"
            lblNhanVien.Text = $"Nhân viên: {_data.TenNhanVien}";
            lblNgay.Text = $"Giờ tạo: {_data.NgayTao:dd/MM/yyyy HH:mm}";

            // 3. Bind Thông tin giao hàng
            lblThongTinGiaoHang.Text = _data.GhiChu; // Lỗi CS1061 đã được fix (ở file Dto)

            // 4. Bind Danh sách
            dgItems.ItemsSource = _data.ChiTiet;

            // 5. Bind Tiền (Phiếu này không có điểm/tiền khách đưa)
            lblTongTienGoc.Text = _data.TongTienGoc.ToString("N0"); // Lỗi CS0103 đã được fix
            lblGiamGia.Text = $"- {_data.GiamGia:N0}";
            lblThanhTien.Text = _data.ThanhTien.ToString("N0") + " đ";
        }

        // ... (Hàm BtnIn_Click và BtnClose_Click giữ nguyên) ...
        private void BtnIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    panelButtons.Visibility = Visibility.Collapsed;
                    var scrollViewer = printArea.Parent as ScrollViewer;
                    if (scrollViewer == null)
                    {
                        panelButtons.Visibility = Visibility.Visible;
                        return;
                    }
                    var originalContent = scrollViewer.Content;
                    scrollViewer.Content = null;
                    printArea.Width = printDialog.PrintableAreaWidth;
                    printArea.Height = double.NaN;
                    printArea.Measure(new Size(printDialog.PrintableAreaWidth, double.PositiveInfinity));
                    printArea.Arrange(new Rect(new Point(0, 0), printArea.DesiredSize));
                    printDialog.PrintVisual(printArea, "Phiếu Giao Hàng CafeBook");
                    printArea.Width = 380;
                    printArea.Height = double.NaN;
                    scrollViewer.Content = originalContent;
                    panelButtons.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi in: {ex.Message}");
                if (panelButtons != null) panelButtons.Visibility = Visibility.Visible;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}