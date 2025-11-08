using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using CafebookModel.Model.ModelApp.NhanVien;

namespace AppCafebookApi.View.common
{
    public partial class PhieuTraPreviewWindow : Window
    {
        private static readonly HttpClient httpClient;
        private readonly int _idPhieuTra;

        static PhieuTraPreviewWindow()
        {
            httpClient = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5166") };
        }

        public PhieuTraPreviewWindow(int idPhieuTra)
        {
            InitializeComponent();
            _idPhieuTra = idPhieuTra;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = await httpClient.GetFromJsonAsync<PhieuTraPrintDto>($"api/app/nhanvien/thuesach/print-data/tra/{_idPhieuTra}");
                if (data == null)
                {
                    MessageBox.Show("Không tìm thấy dữ liệu phiếu trả.");
                    this.Close();
                    return;
                }

                // Đổ dữ liệu vào UI
                lblTenQuan.Text = data.TenQuan;
                lblDiaChiQuan.Text = data.DiaChiQuan;
                lblSdtQuan.Text = $"SĐT: {data.SdtQuan}";
                lblMaPhieu.Text = $"Mã: {data.IdPhieuTra}";
                lblNgayTao.Text = $"Ngày: {data.NgayTra:dd/MM/yyyy HH:mm}";

                lblTenKhach.Text = data.TenKhachHang;
                lblSdtKhach.Text = $"SĐT: {data.SdtKhachHang}";
                lblTenNhanVien.Text = data.TenNhanVien;
                lblPhieuThueGoc.Text = data.IdPhieuThue;

                dgChiTiet.ItemsSource = data.ChiTiet;

                lblTongCoc.Text = $"{data.TongTienCoc:N0} đ";
                lblPhiThue.Text = $"- {data.TongPhiThue:N0} đ";
                lblTongPhat.Text = $"- {data.TongTienPhat:N0} đ";
                lblTongHoanTra.Text = $"{data.TongHoanTra:N0} đ";
                lblDiemTichLuy.Text = $"+ {data.DiemTichLuy}";
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
                    (sender as Button)!.Visibility = Visibility.Collapsed;
                    (FindName("BtnClose") as Button)!.Visibility = Visibility.Collapsed;

                    printDialog.PrintVisual(printArea, "In Phiếu Trả Sách");

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