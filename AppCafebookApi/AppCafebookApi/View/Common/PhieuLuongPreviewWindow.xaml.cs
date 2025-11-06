using System;
using System.Linq; // Cần thêm
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AppCafebookApi.Services;
using CafebookModel.Model.ModelApp;

namespace AppCafebookApi.View.Common
{
    public partial class PhieuLuongPreviewWindow : Window
    {
        private static readonly HttpClient httpClient;
        private int _idPhieuLuong;
        private PhieuLuongChiTietDto? _phieuLuong;
        private CaiDatThongTinCuaHangDto? _thongTinCuaHang; // Thêm đối tượng mới

        static PhieuLuongPreviewWindow()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public PhieuLuongPreviewWindow(int idPhieuLuong)
        {
            InitializeComponent();
            _idPhieuLuong = idPhieuLuong;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            // Thực hiện song song 2 tác vụ tải dữ liệu
            var loadPhieuTask = LoadChiTietPhieuLuongAsync();
            var loadInfoTask = LoadThongTinCuaHangAsync();

            await Task.WhenAll(loadPhieuTask, loadInfoTask);

            // Gán DataContext sau khi cả hai đã tải xong
            if (_phieuLuong != null && _thongTinCuaHang != null)
            {
                // Gán chi tiết phiếu lương cho khu vực chính (printArea)
                printArea.DataContext = _phieuLuong;

                // Gán thông tin cửa hàng cho khu vực header (headerPanel)
                headerPanel.DataContext = _thongTinCuaHang;

                // Xử lý logic nút bấm (nếu cần)
                if (_phieuLuong.TrangThai == "Đã phát")
                {
                    btnXacNhanPhat.IsEnabled = false;
                    if (btnXacNhanPhat.Content is StackPanel sp)
                    {
                        var tb = sp.Children.OfType<TextBlock>().FirstOrDefault();
                        if (tb != null)
                        {
                            tb.Text = "Đã Phát";
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Không thể tải đầy đủ thông tin phiếu lương hoặc thông tin cửa hàng.", "Lỗi API");
                this.Close();
            }

            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Tải chi tiết phiếu lương (API 1)
        /// </summary>
        private async Task LoadChiTietPhieuLuongAsync()
        {
            try
            {
                var response = await httpClient.GetAsync($"api/app/phatluong/chitiet/{_idPhieuLuong}");
                if (response.IsSuccessStatusCode)
                {
                    _phieuLuong = await response.Content.ReadFromJsonAsync<PhieuLuongChiTietDto>();
                }
                else
                {
                    // Xử lý lỗi ở hàm Window_Loaded
                }
            }
            catch (Exception)
            {
                // Xử lý lỗi ở hàm Window_Loaded
            }
        }

        /// <summary>
        /// Tải thông tin cửa hàng (API 2)
        /// </summary>
        private async Task LoadThongTinCuaHangAsync()
        {
            try
            {
                var response = await httpClient.GetAsync("api/app/caidat/thong-tin-cua-hang");
                if (response.IsSuccessStatusCode)
                {
                    _thongTinCuaHang = await response.Content.ReadFromJsonAsync<CaiDatThongTinCuaHangDto>();
                }
                else
                {
                    // Nếu thất bại, tạo đối tượng rỗng để tránh lỗi
                    _thongTinCuaHang = new CaiDatThongTinCuaHangDto { TenQuan = "N/A", DiaChi = "N/A", SoDienThoai = "N/A" };
                }
            }
            catch (Exception)
            {
                _thongTinCuaHang = new CaiDatThongTinCuaHangDto { TenQuan = "Lỗi", DiaChi = "Lỗi", SoDienThoai = "Lỗi" };
            }
        }

        // ... (Các hàm BtnXacNhanPhat_Click, BtnPrint_Click, BtnClose_Click giữ nguyên y hệt) ...

        private async void BtnXacNhanPhat_Click(object sender, RoutedEventArgs e)
        {
            if (_phieuLuong == null || AuthService.CurrentUser == null)
            {
                MessageBox.Show("Lỗi: Không có thông tin phiếu lương hoặc người dùng.", "Lỗi");
                return;
            }

            var confirm = MessageBox.Show($"Xác nhận phát lương cho:\n\nNhân viên: {_phieuLuong.HoTenNhanVien}\nSố tiền: {_phieuLuong.ThucLanh:N0} đ",
                                        "Xác nhận Phát Lương", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.No) return;

            LoadingOverlay.Visibility = Visibility.Visible;

            var dto = new PhatLuongXacNhanDto
            {
                IdNguoiPhat = AuthService.CurrentUser.IdNhanVien
            };

            try
            {
                var response = await httpClient.PutAsJsonAsync($"api/app/phatluong/xacnhan/{_idPhieuLuong}", dto);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xác nhận phát lương thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var scrollViewer = printArea.Parent as ScrollViewer;
                    if (scrollViewer != null)
                    {
                        scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    }

                    printDialog.PrintVisual(printArea, "In Phiếu Lương");

                    if (scrollViewer != null)
                    {
                        scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi in: {ex.Message}", "Lỗi In ấn");
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}