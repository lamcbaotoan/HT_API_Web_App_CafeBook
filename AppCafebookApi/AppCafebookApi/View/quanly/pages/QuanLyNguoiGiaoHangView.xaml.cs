using AppCafebookApi.Services;
using CafebookModel.Model.ModelApp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyNguoiGiaoHangView : Page
    {
        private int _selectedId = 0;

        public QuanLyNguoiGiaoHangView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                var list = await ApiClient.Instance.GetFromJsonAsync<List<NguoiGiaoHangCrudDto>>("api/app/nguoigiaohang/all");
                dgShipper.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}");
            }
        }

        private void DgShipper_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgShipper.SelectedItem is NguoiGiaoHangCrudDto item)
            {
                _selectedId = item.IdNguoiGiaoHang;
                txtTen.Text = item.TenNguoiGiaoHang;
                txtSdt.Text = item.SoDienThoai;
                cmbTrangThai.Text = item.TrangThai;

                btnAdd.IsEnabled = false;
                btnEdit.IsEnabled = true;
                btnDelete.IsEnabled = true;
            }
        }

        private void ClearForm()
        {
            _selectedId = 0;
            txtTen.Text = "";
            txtSdt.Text = "";
            cmbTrangThai.SelectedIndex = 0;
            dgShipper.SelectedItem = null;

            btnAdd.IsEnabled = true;
            btnEdit.IsEnabled = false;
            btnDelete.IsEnabled = false;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e) => ClearForm();
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTen.Text)) return;

            var dto = new NguoiGiaoHangCrudDto
            {
                TenNguoiGiaoHang = txtTen.Text,
                SoDienThoai = txtSdt.Text,
                TrangThai = cmbTrangThai.Text
            };

            try
            {
                var response = await ApiClient.Instance.PostAsJsonAsync("api/app/nguoigiaohang", dto);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Thêm thành công!");
                    await LoadDataAsync();
                    ClearForm();
                }
                else
                {
                    MessageBox.Show("Lỗi: " + await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedId == 0) return;

            var dto = new NguoiGiaoHangCrudDto
            {
                IdNguoiGiaoHang = _selectedId,
                TenNguoiGiaoHang = txtTen.Text,
                SoDienThoai = txtSdt.Text,
                TrangThai = cmbTrangThai.Text
            };

            try
            {
                var response = await ApiClient.Instance.PutAsJsonAsync($"api/app/nguoigiaohang/{_selectedId}", dto);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Cập nhật thành công!");
                    await LoadDataAsync();
                    ClearForm();
                }
                else { MessageBox.Show("Lỗi: " + await response.Content.ReadAsStringAsync()); }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedId == 0) return;
            if (MessageBox.Show("Bạn có chắc muốn xóa?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.No) return;

            try
            {
                var response = await ApiClient.Instance.DeleteAsync($"api/app/nguoigiaohang/{_selectedId}");
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xóa thành công!");
                    await LoadDataAsync();
                    ClearForm();
                }
                else { MessageBox.Show("Lỗi: " + await response.Content.ReadAsStringAsync()); }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }
    }
}