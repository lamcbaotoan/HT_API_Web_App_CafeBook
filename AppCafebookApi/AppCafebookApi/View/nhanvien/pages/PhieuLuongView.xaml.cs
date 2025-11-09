// Tệp: AppCafebookApi/View/nhanvien/pages/PhieuLuongView.xaml.cs
using AppCafebookApi.Services;
using CafebookModel.Model.ModelApp.NhanVien;
using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class PhieuLuongView : Page
    {
        public PhieuLuongView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDanhSachPhieuLuongAsync();
        }

        /// <summary>
        /// Tải danh sách các phiếu lương (cột bên trái)
        /// </summary>
        private async Task LoadDanhSachPhieuLuongAsync()
        {
            try
            {
                var response = await ApiClient.Instance.GetFromJsonAsync<PhieuLuongViewDto>("api/app/nhanvien/phieuluong/list");
                if (response != null && response.DanhSachPhieuLuong.Any())
                {
                    lbPhieuLuong.ItemsSource = response.DanhSachPhieuLuong;
                    lbPhieuLuong.SelectedIndex = 0; // Tự động chọn phiếu mới nhất
                }
                else
                {
                    panelChonPhieu.Visibility = Visibility.Visible;
                    panelChiTiet.Visibility = Visibility.Collapsed;
                    (panelChonPhieu.Children[0] as TextBlock).Text = "Không tìm thấy phiếu lương nào.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách phiếu lương: {ex.Message}", "Lỗi API");
            }
        }

        /// <summary>
        /// Tải chi tiết của phiếu lương được chọn (cột bên phải)
        /// </summary>
        private async Task LoadChiTietPhieuLuongAsync(int idPhieuLuong)
        {
            try
            {
                var data = await ApiClient.Instance.GetFromJsonAsync<PhieuLuongChiTietDto>($"api/app/nhanvien/phieuluong/detail/{idPhieuLuong}");
                if (data == null)
                {
                    MessageBox.Show("Không thể tải chi tiết phiếu lương.", "Lỗi");
                    return;
                }

                // Điền thông tin
                lblTieuDeChiTiet.Text = $"Chi tiết phiếu lương tháng {data.Thang}/{data.Nam}";
                lblThucLanh.Text = data.ThucLanh.ToString("N0") + " đ";

                // Xử lý trạng thái
                if (data.TrangThai == "Đã phát")
                {
                    lblTrangThai.Text = $"Đã phát ngày {data.NgayPhatLuong:dd/MM/yyyy} bởi {data.TenNguoiPhat ?? "Quản lý"}";
                    lblTrangThai.Foreground = (SolidColorBrush)FindResource("GreenBrush");
                }
                else // Đã chốt
                {
                    lblTrangThai.Text = "Đã chốt (Chưa phát lương)";
                    lblTrangThai.Foreground = (SolidColorBrush)FindResource("TextGrayBrush");
                }

                // Lương cơ bản
                lblLuongCoBan.Text = data.LuongCoBan.ToString("N0") + " đ / giờ";
                lblTongGioLam.Text = data.TongGioLam.ToString("N2") + " giờ";
                lblTienLuongTheoGio.Text = data.TienLuongTheoGio.ToString("N0") + " đ";

                // Thưởng
                dgThuong.ItemsSource = data.DanhSachThuong;
                lblTongThuong.Text = $"Tổng thưởng: {data.TongTienThuong:N0} đ";

                // Phạt
                dgKhauTru.ItemsSource = data.DanhSachPhat;
                lblTongKhauTru.Text = $"Tổng khấu trừ: {data.TongKhauTru:N0} đ"; // Hiển thị số âm

                // Tổng kết
                lblThucLanhFinal.Text = data.ThucLanh.ToString("N0") + " đ";

                // Hiển thị panel
                panelChonPhieu.Visibility = Visibility.Collapsed;
                panelChiTiet.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết phiếu lương: {ex.Message}", "Lỗi API");
                panelChonPhieu.Visibility = Visibility.Visible;
                panelChiTiet.Visibility = Visibility.Collapsed;
            }
        }

        private async void LbPhieuLuong_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbPhieuLuong.SelectedItem is PhieuLuongItemDto selectedItem)
            {
                await LoadChiTietPhieuLuongAsync(selectedItem.IdPhieuLuong);
            }
        }
    }
}