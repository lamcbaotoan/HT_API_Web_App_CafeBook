using CafebookModel.Model.ModelApp.NhanVien;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AppCafebookApi.View.common
{
    public partial class HoaDonPreviewWindow : Window
    {
        private readonly HoaDonPreviewDto _data;

        public HoaDonPreviewWindow(HoaDonPreviewDto data)
        {
            InitializeComponent();
            _data = data;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Bind Thông tin quán
            lblTenQuan.Text = _data.TenQuan;
            lblDiaChi.Text = _data.DiaChi;
            lblSoDienThoai.Text = $"SĐT: {_data.SoDienThoai}";
            lblWifi.Text = $"Wifi: {_data.WifiMatKhau}";

            // 2. Bind Thông tin Hóa đơn
            lblTieuDe.Text = _data.IsProvisional ? "PHIẾU TẠM TÍNH" : "HÓA ĐƠN THANH TOÁN";
            lblSoHoaDon.Text = $"Số HĐ: #{_data.IdHoaDon}";
            lblBan.Text = $"Bàn: {_data.SoBan}";
            lblNhanVien.Text = $"Nhân viên: {_data.TenNhanVien}";
            lblNgay.Text = $"Giờ vào: {_data.ThoiGianTao:dd/MM/yyyy HH:mm}";
            lblKhachHang.Text = $"Khách: {_data.TenKhachHang}";

            // 3. Bind Danh sách
            dgItems.ItemsSource = _data.Items;
            icPhuThu.ItemsSource = _data.Surcharges;

            // 4. Bind Tiền
            lblTongTienGoc.Text = _data.TongTienGoc.ToString("N0");
            lblGiamGiaKM.Text = $"- {_data.GiamGiaKM:N0}";
            lblGiamGiaDiem.Text = $"- {_data.GiamGiaDiem:N0}";
            lblTongPhuThu.Text = $"+ {_data.TongPhuThu:N0}";
            lblThanhTien.Text = _data.ThanhTien.ToString("N0") + " đ";

            // Sửa 1: Ẩn/hiện panel tiền mặt
            if (_data.PhuongThucThanhToan == "Tiền mặt")
            {
                lblKhachDua.Text = _data.KhachDua.ToString("N0");
                lblTienThoi.Text = _data.TienThoi.ToString("N0");
            }
            else
            {
                gridTienMat.Visibility = Visibility.Collapsed;
                gridTienThoi.Visibility = Visibility.Collapsed;
            }
        }

        // ### SỬA LỖI IN BỊ CẮT (YÊU CẦU 3) ###
        private void BtnIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // 1. Ẩn các nút
                    panelButtons.Visibility = Visibility.Collapsed;

                    // 2. Lấy ScrollViewer và Border (printArea)
                    var scrollViewer = printArea.Parent as ScrollViewer;
                    if (scrollViewer == null)
                    {
                        panelButtons.Visibility = Visibility.Visible;
                        return;
                    }

                    // 3. TẠM THỜI gỡ Border (printArea) ra khỏi ScrollViewer
                    //    và cho nó tự động co giãn theo nội dung
                    var originalContent = scrollViewer.Content;
                    scrollViewer.Content = null;

                    // 4. Đặt lại kích thước để nó tự co giãn theo nội dung
                    printArea.Width = printDialog.PrintableAreaWidth; // Vừa khổ giấy
                    printArea.Height = double.NaN; // Tự động co giãn chiều cao

                    // 5. Cập nhật layout để tính toán lại kích thước thật
                    printArea.Measure(new Size(printDialog.PrintableAreaWidth, double.PositiveInfinity));
                    printArea.Arrange(new Rect(new Point(0, 0), printArea.DesiredSize));

                    // 6. In
                    printDialog.PrintVisual(printArea, "Hóa đơn CafeBook");

                    // 7. Trả lại
                    printArea.Width = 380; // Trả lại width 380 (từ XAML)
                    printArea.Height = double.NaN; // Reset
                    scrollViewer.Content = originalContent;
                    panelButtons.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi in: {ex.Message}");
                // Đảm bảo các nút hiện lại nếu có lỗi
                if (panelButtons != null) panelButtons.Visibility = Visibility.Visible;
            }
        }

        // ### THÊM NÚT ĐÓNG (YÊU CẦU 1 & 2) ###
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}