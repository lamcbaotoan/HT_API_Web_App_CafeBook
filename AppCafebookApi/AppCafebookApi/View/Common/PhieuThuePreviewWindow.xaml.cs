using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using CafebookModel.Model.ModelApp.NhanVien;

namespace AppCafebookApi.View.common
{
    public partial class PhieuThuePreviewWindow : Window
    {
        private static readonly HttpClient httpClient;
        private readonly int _idPhieuThue;

        static PhieuThuePreviewWindow()
        {
            httpClient = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5166") };
        }

        public PhieuThuePreviewWindow(int idPhieuThue)
        {
            InitializeComponent();
            _idPhieuThue = idPhieuThue;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = await httpClient.GetFromJsonAsync<PhieuThuePrintDto>($"api/app/nhanvien/thuesach/print-data/{_idPhieuThue}");
                if (data == null)
                {
                    MessageBox.Show("Không tìm thấy dữ liệu phiếu thuê.");
                    this.Close();
                    return;
                }

                // Đổ dữ liệu vào UI
                lblTenQuan.Text = data.TenQuan;
                lblDiaChiQuan.Text = data.DiaChiQuan;
                lblSdtQuan.Text = $"SĐT: {data.SdtQuan}";
                lblMaPhieu.Text = $"Mã: {data.IdPhieu}";
                lblNgayTao.Text = $"Ngày: {data.NgayTao:dd/MM/yyyy HH:mm}";

                lblTenKhach.Text = data.TenKhachHang;
                lblSdtKhach.Text = $"SĐT: {data.SdtKhachHang}";
                lblTenNhanVien.Text = data.TenNhanVien;
                lblNgayHenTra.Text = $"{data.NgayHenTra:dd/MM/yyyy}";

                dgChiTiet.ItemsSource = data.ChiTiet;

                lblPhiThue.Text = $"{data.TongPhiThue:N0} đ";
                lblTongCoc.Text = $"{data.TongTienCoc:N0} đ";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu in: {ex.Message}", "Lỗi API");
                this.Close();
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Tạm ẩn 2 nút để khi in không bị thấy
                    (sender as Button)!.Visibility = Visibility.Collapsed;
                    (FindName("BtnClose") as Button)!.Visibility = Visibility.Collapsed;

                    printDialog.PrintVisual(printArea, "In Phiếu Thuê Sách");

                    // Hiện lại 2 nút
                    (sender as Button)!.Visibility = Visibility.Visible;
                    (FindName("BtnClose") as Button)!.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi in: {ex.Message}", "Lỗi");
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}