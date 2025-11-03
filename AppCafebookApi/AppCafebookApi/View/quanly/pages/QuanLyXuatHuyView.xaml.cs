using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;
using System.Net;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Globalization;
using AppCafebookApi.Services;

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyXuatHuyView : Page
    {
        private static readonly HttpClient httpClient;

        private List<PhieuXuatHuyDto> _phieuXuatHuyList = new List<PhieuXuatHuyDto>();
        private ObservableCollection<ChiTietPhieuXuatHuyDto> _chiTietPhieuHuyList = new ObservableCollection<ChiTietPhieuXuatHuyDto>();
        private bool _isCreatingPhieuHuy = false;

        // Cache
        private List<NguyenLieuCrudDto> _nguyenLieuList = new List<NguyenLieuCrudDto>(); // Dùng cho ComboBox

        static QuanLyXuatHuyView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyXuatHuyView()
        {
            InitializeComponent();
            dpTuNgay_Phieu.SelectedDate = DateTime.Today.AddDays(-30);
            dpDenNgay_Phieu.SelectedDate = DateTime.Today;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            await LoadNguyenLieuAsync();
            await LoadPhieuHuyAsync();
            ResetPhieuHuyForm();
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        #region Tải Dữ Liệu

        private async Task LoadNguyenLieuAsync()
        {
            try
            {
                _nguyenLieuList = (await httpClient.GetFromJsonAsync<List<NguyenLieuCrudDto>>("api/app/kho/nguyenlieu")) ?? new List<NguyenLieuCrudDto>();

                var nlList = _nguyenLieuList.Select(nl => new { nl.IdNguyenLieu, TenNguyenLieu = $"{nl.TenNguyenLieu} (Tồn: {nl.TonKho:N2} {nl.DonViTinh})" }).ToList();
                nlList.Insert(0, new { IdNguyenLieu = 0, TenNguyenLieu = "-- Chọn Nguyên Liệu --" });
                colNguyenLieu.ItemsSource = nlList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải nguyên liệu: {ex.Message}", "Lỗi API");
            }
        }

        private async Task LoadPhieuHuyAsync()
        {
            DateTime? start = dpTuNgay_Phieu.SelectedDate;
            DateTime? end = dpDenNgay_Phieu.SelectedDate;

            try
            {
                string url = "api/app/kho/phieuxuathuy";
                if (start.HasValue && end.HasValue)
                {
                    url += $"?startDate={start.Value:yyyy-MM-dd}&endDate={end.Value:yyyy-MM-dd}";
                }

                _phieuXuatHuyList = (await httpClient.GetFromJsonAsync<List<PhieuXuatHuyDto>>(url)) ?? new List<PhieuXuatHuyDto>();
                dgPhieuXuatHuy.ItemsSource = _phieuXuatHuyList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải phiếu hủy: {ex.Message}", "Lỗi API");
            }
        }

        #endregion

        #region Xử lý Form

        private void ResetPhieuHuyForm()
        {
            _isCreatingPhieuHuy = false;
            dgPhieuXuatHuy.SelectedItem = null;

            dpNgayHuy.SelectedDate = DateTime.Today;
            txtLyDoHuy.Text = "";
            lblTongGiaTriHuy.Text = "0 VND";

            _chiTietPhieuHuyList.Clear();
            dgChiTietPhieuHuy.ItemsSource = _chiTietPhieuHuyList;

            btnLuuPhieuHuy.IsEnabled = false;
            btnThemDong.IsEnabled = false;
            dgChiTietPhieuHuy.IsReadOnly = true;
        }

        private void BtnTaoPhieuMoi_Click(object sender, RoutedEventArgs e)
        {
            ResetPhieuHuyForm();
            _isCreatingPhieuHuy = true;
            btnLuuPhieuHuy.IsEnabled = true;
            btnThemDong.IsEnabled = true;
            dgChiTietPhieuHuy.IsReadOnly = false;
        }

        private async void BtnLocPhieu_Click(object sender, RoutedEventArgs e)
        {
            await LoadPhieuHuyAsync();
        }

        private async void DgPhieuXuatHuy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPhieuXuatHuy.SelectedItem is not PhieuXuatHuyDto selected)
            {
                ResetPhieuHuyForm();
                return;
            }

            _isCreatingPhieuHuy = false;
            btnLuuPhieuHuy.IsEnabled = false;
            btnThemDong.IsEnabled = false;
            dgChiTietPhieuHuy.IsReadOnly = true;

            try
            {
                var details = await httpClient.GetFromJsonAsync<List<ChiTietPhieuXuatHuyDto>>($"api/app/kho/phieuxuathuy/{selected.IdPhieuXuatHuy}");
                _chiTietPhieuHuyList = new ObservableCollection<ChiTietPhieuXuatHuyDto>(details ?? new List<ChiTietPhieuXuatHuyDto>());
                dgChiTietPhieuHuy.ItemsSource = _chiTietPhieuHuyList;

                dpNgayHuy.SelectedDate = selected.NgayXuatHuy;
                txtLyDoHuy.Text = selected.LyDoXuatHuy;
                lblTongGiaTriHuy.Text = selected.TongGiaTriHuy.ToString("N0") + " VND";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết phiếu hủy: {ex.Message}", "Lỗi API");
            }
        }

        private void BtnThemDong_Click(object sender, RoutedEventArgs e)
        {
            _chiTietPhieuHuyList.Add(new ChiTietPhieuXuatHuyDto { SoLuong = 1 });
            dgChiTietPhieuHuy.ItemsSource = _chiTietPhieuHuyList;
        }

        private void DgChiTietPhieuHuy_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Không cần tính tổng, API sẽ tự tính
        }

        private async void BtnLuuPhieuHuy_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCreatingPhieuHuy || _chiTietPhieuHuyList.Count == 0)
            {
                MessageBox.Show("Vui lòng 'Tạo Phiếu Mới' và 'Thêm Dòng' nguyên liệu trước khi lưu.", "Lỗi");
                return;
            }
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Lỗi phiên đăng nhập. Vui lòng đăng nhập lại.", "Lỗi");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtLyDoHuy.Text))
            {
                MessageBox.Show("Lý do hủy là bắt buộc.", "Lỗi");
                return;
            }

            var dto = new PhieuXuatHuyCreateDto
            {
                IdNhanVien = AuthService.CurrentUser.IdNhanVien,
                NgayXuatHuy = dpNgayHuy.SelectedDate ?? DateTime.Today,
                LyDoXuatHuy = txtLyDoHuy.Text,
                ChiTiet = _chiTietPhieuHuyList.Where(ct => ct.IdNguyenLieu > 0 && ct.SoLuong > 0)
                                              .Select(ct => new ChiTietPhieuXuatHuyCreateDto
                                              {
                                                  IdNguyenLieu = ct.IdNguyenLieu,
                                                  SoLuong = ct.SoLuong
                                              }).ToList()
            };

            if (!dto.ChiTiet.Any())
            {
                MessageBox.Show("Phiếu hủy phải có ít nhất 1 nguyên liệu hợp lệ.", "Lỗi");
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/kho/phieuxuathuy", dto);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu phiếu xuất hủy thành công!", "Thông báo");
                    await LoadPhieuHuyAsync();
                    ResetPhieuHuyForm();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService?.CanGoBack == true)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}