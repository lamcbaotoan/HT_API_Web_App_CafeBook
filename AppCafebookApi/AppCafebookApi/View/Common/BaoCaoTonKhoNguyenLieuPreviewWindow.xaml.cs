using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;
using System.IO;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq; // Cần NuGet Newtonsoft.Json
using System.Globalization;
using System.Text.Json; // THÊM

namespace AppCafebookApi.View.common
{
    public partial class BaoCaoTonKhoNguyenLieuPreviewWindow : Page
    {
        private static readonly HttpClient httpClient;
        private BaoCaoTonKhoTongHopDto? currentReportData;

        static BaoCaoTonKhoNguyenLieuPreviewWindow()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public BaoCaoTonKhoNguyenLieuPreviewWindow()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFiltersAsync();
            await GenerateReportAsync();
        }

        private async Task LoadFiltersAsync()
        {
            try
            {
                // SỬA: Dùng GetFromJsonAsync<JsonElement?>
                var response = await httpClient.GetFromJsonAsync<JsonElement?>("api/app/baocaohieusuat/filters");

                if (!response.HasValue) // SỬA: Dùng .HasValue
                {
                    MessageBox.Show("Không nhận được dữ liệu lọc.", "Lỗi API");
                    return;
                }

                // SỬA: Dùng .GetProperty().Deserialize<>()
                var vaiTros = response.Value.GetProperty("vaiTros").Deserialize<List<FilterLookupDto>>() ?? new List<FilterLookupDto>();

                vaiTros.Insert(0, new FilterLookupDto { Id = 0, Ten = "Tất cả Vai trò" });
                cmbNhaCungCap.ItemsSource = vaiTros;
                cmbNhaCungCap.SelectedValue = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải bộ lọc: {ex.Message}", "Lỗi API");
            }
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            await GenerateReportAsync();
        }

        private async Task GenerateReportAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            btnGenerate.IsEnabled = false;

            var request = new BaoCaoTonKhoRequestDto
            {
                SearchText = string.IsNullOrEmpty(txtSearch.Text) ? null : txtSearch.Text,
                NhaCungCapId = (int)cmbNhaCungCap.SelectedValue == 0 ? (int?)null : (int)cmbNhaCungCap.SelectedValue,
                ShowLowStockOnly = chkLowStockOnly.IsChecked ?? false
            };

            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/baocaokho/report", request);
                if (response.IsSuccessStatusCode)
                {
                    currentReportData = await response.Content.ReadFromJsonAsync<BaoCaoTonKhoTongHopDto>();
                    if (currentReportData != null)
                    {
                        PopulateUi(currentReportData);
                    }
                }
                else
                {
                    MessageBox.Show($"Lỗi API: {response.ReasonPhrase}", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối API: {ex.Message}", "Lỗi");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                btnGenerate.IsEnabled = true;
            }
        }

        private void PopulateUi(BaoCaoTonKhoTongHopDto data)
        {
            var culture = CultureInfo.GetCultureInfo("vi-VN");

            // 1. Cập nhật Thẻ KPI
            lblGiaTriTonKho.Text = data.Kpi.TongGiaTriTonKho.ToString("C0", culture);
            lblSPSapHet.Text = data.Kpi.SoLuongSPSapHet.ToString();
            lblGiaTriDaHuy.Text = data.Kpi.TongGiaTriDaHuy.ToString("C0", culture);

            // 2. Cập nhật các Tab
            dgInventory.ItemsSource = data.ChiTietTonKho;
            dgAuditHistory.ItemsSource = data.LichSuKiemKe;
            dgWasteHistory.ItemsSource = data.LichSuHuyHang;
        }

        private async void BtnExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            if (currentReportData == null)
            {
                MessageBox.Show("Chưa có dữ liệu báo cáo để xuất. Vui lòng nhấn 'Lọc Báo Cáo' trước.", "Chưa có dữ liệu");
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"BaoCaoTonKhoNguyenLieu_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;
                    await CreateExcelReportAsync(sfd.FileName, currentReportData);
                    LoadingOverlay.Visibility = Visibility.Collapsed;

                    MessageBox.Show($"Đã xuất báo cáo thành công!\n\nĐường dẫn: {sfd.FileName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    MessageBox.Show($"Có lỗi khi xuất Excel: {ex.Message}\n\nĐảm bảo bạn đã đóng file Excel (nếu nó đang mở).", "Lỗi Xuất File", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Dán đè lên hàm CreateExcelReportAsync CŨ của bạn
        private async Task CreateExcelReportAsync(string filePath, BaoCaoTonKhoTongHopDto data)
        {
            ExcelPackage.License.SetNonCommercialPersonal("CafeBook");
            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Exists)
            {
                fileInfo.Delete(); // Xóa file cũ nếu tồn tại để tránh lỗi
            }

            using (var package = new ExcelPackage(fileInfo))
            {
                // --- 1. ĐỊNH NGHĨA CÁC STYLE TÁI SỬ DỤNG ---

                // Định dạng tiền tệ VND
                var currencyFormat = "#,##0 \"đ\"";
                // Định dạng số nguyên
                var numberFormat = "#,##0";
                // Định dạng số thập phân (cho kg, lít,...)
                var decimalFormat = "#,##0.00";
                // Định dạng ngày
                var dateFormat = "dd/MM/yyyy";

                // Style cho Tiêu đề chính (Merge cell, to, đậm, nền xanh)
                Action<ExcelRange> styleMainTitle = (range) =>
                {
                    range.Merge = true;
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 16;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#333366")); // Xanh đậm
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                };

                // Style cho Tiêu đề phụ (Merge cell, nghiêng)
                Action<ExcelRange> styleSubTitle = (range) =>
                {
                    range.Merge = true;
                    range.Style.Font.Italic = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                };

                // --- 2. SHEET 1: TỔNG QUAN KHO (Sheet MỚI) ---
                var wsTongQuan = package.Workbook.Worksheets.Add("Tổng Quan Kho");

                // Tiêu đề
                wsTongQuan.Cells["A1:C1"].Value = "BÁO CÁO TỔNG QUAN KHO";
                styleMainTitle(wsTongQuan.Cells["A1:C1"]);

                wsTongQuan.Cells["A2:C2"].Value = $"Báo cáo tạo lúc: {DateTime.Now:dd/MM/yyyy HH:mm}";
                styleSubTitle(wsTongQuan.Cells["A2:C2"]);

                // Hiển thị KPIs
                wsTongQuan.Cells["A4"].Value = "CHỈ SỐ QUAN TRỌNG";
                wsTongQuan.Cells["A4:B4"].Merge = true;
                wsTongQuan.Cells["A4:B4"].Style.Font.Bold = true;
                wsTongQuan.Cells["A4:B4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                wsTongQuan.Cells["A4:B4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                wsTongQuan.Cells["A5"].Value = "Tổng Giá Trị Tồn Kho";
                wsTongQuan.Cells["B5"].Value = data.Kpi.TongGiaTriTonKho;
                wsTongQuan.Cells["B5"].Style.Numberformat.Format = currencyFormat;
                wsTongQuan.Cells["B5"].Style.Font.Bold = true;

                wsTongQuan.Cells["A6"].Value = "SL Nguyên Liệu Sắp Hết";
                wsTongQuan.Cells["B6"].Value = data.Kpi.SoLuongSPSapHet;
                wsTongQuan.Cells["B6"].Style.Numberformat.Format = numberFormat;

                // Tô màu cảnh báo nếu có hàng sắp hết
                if (data.Kpi.SoLuongSPSapHet > 0)
                {
                    wsTongQuan.Cells["B6"].Style.Font.Color.SetColor(System.Drawing.Color.OrangeRed);
                    wsTongQuan.Cells["B6"].Style.Font.Bold = true;
                }

                wsTongQuan.Cells["A7"].Value = "Giá Trị Hủy (30 ngày qua)";
                wsTongQuan.Cells["B7"].Value = data.Kpi.TongGiaTriDaHuy;
                wsTongQuan.Cells["B7"].Style.Numberformat.Format = currencyFormat;

                // Tô màu đỏ nếu có hàng bị hủy
                if (data.Kpi.TongGiaTriDaHuy > 0)
                {
                    wsTongQuan.Cells["B7"].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    wsTongQuan.Cells["B7"].Style.Font.Bold = true;
                }

                // Kẻ viền cho bảng KPI
                wsTongQuan.Cells["A4:B7"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
                wsTongQuan.Cells[wsTongQuan.Dimension.Address].AutoFitColumns();

                // --- 3. SHEET 2: CHI TIẾT TỒN KHO ---
                var ws1 = package.Workbook.Worksheets.Add("Chi Tiết Tồn Kho");
                ws1.Cells["A1:E1"].Value = "Báo cáo Tồn kho Nguyên liệu Chi tiết";
                styleMainTitle(ws1.Cells["A1:E1"]);

                // Load dữ liệu vào Bảng (Table)
                ws1.Cells["A3"].LoadFromCollection(data.ChiTietTonKho, true, OfficeOpenXml.Table.TableStyles.Medium9);
                var tbl1 = ws1.Tables[0];

                // Định dạng số cho các cột trong bảng
                ws1.Cells[tbl1.Address.Start.Row + 1, tbl1.Columns["tonKho"].Position + 1, tbl1.Address.End.Row, tbl1.Columns["tonKho"].Position + 1].Style.Numberformat.Format = decimalFormat;
                ws1.Cells[tbl1.Address.Start.Row + 1, tbl1.Columns["TonKhoToiThieu"].Position + 1, tbl1.Address.End.Row, tbl1.Columns["TonKhoToiThieu"].Position + 1].Style.Numberformat.Format = decimalFormat;

                // *** ĐỊNH DẠNG CÓ ĐIỀU KIỆN (CONDITIONAL FORMATTING) ***
                // Tự động tô màu các dòng dựa trên "TinhTrang"
                var tinhTrangCol = tbl1.Columns["TinhTrang"].Position + 1;
                // Vùng áp dụng: toàn bộ bảng
                var dataRange1 = new ExcelAddress(tbl1.Address.Start.Row + 1, 1, tbl1.Address.End.Row, tbl1.Address.End.Column);

                // Quy tắc 1: Hết hàng
                var ruleHetHang = ws1.ConditionalFormatting.AddExpression(dataRange1);
                ruleHetHang.Formula = $"${ExcelCellAddress.GetColumnLetter(tinhTrangCol)}{dataRange1.Start.Row}=\"Hết hàng\"";
                ruleHetHang.Style.Fill.BackgroundColor.Color = System.Drawing.Color.IndianRed;
                ruleHetHang.Style.Font.Color.Color = System.Drawing.Color.White;

                // Quy tắc 2: Sắp hết
                var ruleSapHet = ws1.ConditionalFormatting.AddExpression(dataRange1);
                ruleSapHet.Formula = $"${ExcelCellAddress.GetColumnLetter(tinhTrangCol)}{dataRange1.Start.Row}=\"Sắp hết\"";
                ruleSapHet.Style.Fill.BackgroundColor.Color = System.Drawing.Color.LightGoldenrodYellow;

                ws1.Cells[ws1.Dimension.Address].AutoFitColumns();

                // --- 4. SHEET 3: LỊCH SỬ KIỂM KÊ ---
                var ws2 = package.Workbook.Worksheets.Add("Lịch Sử Kiểm Kê");
                ws2.Cells["A1:F1"].Value = "Lịch sử Kiểm Kê (Chênh lệch)";
                styleMainTitle(ws2.Cells["A1:F1"]);

                ws2.Cells["A3"].LoadFromCollection(data.LichSuKiemKe, true, OfficeOpenXml.Table.TableStyles.Medium10);
                var tbl2 = ws2.Tables[0];

                // Định dạng cột ngày
                var ngayKiemColRange = ws2.Cells[tbl2.Address.Start.Row + 1, tbl2.Columns["NgayKiem"].Position + 1, tbl2.Address.End.Row, tbl2.Columns["NgayKiem"].Position + 1];
                ngayKiemColRange.Style.Numberformat.Format = dateFormat;

                // Định dạng các cột số lượng
                ws2.Cells[tbl2.Address.Start.Row + 1, tbl2.Columns["TonKhoHeThong"].Position + 1, tbl2.Address.End.Row, tbl2.Columns["TonKhoHeThong"].Position + 1].Style.Numberformat.Format = decimalFormat;
                ws2.Cells[tbl2.Address.Start.Row + 1, tbl2.Columns["TonKhoThucTe"].Position + 1, tbl2.Address.End.Row, tbl2.Columns["TonKhoThucTe"].Position + 1].Style.Numberformat.Format = decimalFormat;

                // Định dạng có điều kiện cho cột Chênh Lệch
                var chenhLechColRange = ws2.Cells[tbl2.Address.Start.Row + 1, tbl2.Columns["ChenhLech"].Position + 1, tbl2.Address.End.Row, tbl2.Columns["ChenhLech"].Position + 1];
                chenhLechColRange.Style.Numberformat.Format = decimalFormat;
                // Tô đỏ nếu < 0 (mất mát)
                var cfRuleLoss = chenhLechColRange.ConditionalFormatting.AddLessThan();
                cfRuleLoss.Formula = "0"; // Gán giá trị so sánh là "0"
                cfRuleLoss.Style.Font.Color.Color = System.Drawing.Color.Red;
                // Tô xanh nếu > 0 (dư)
                var cfRuleGain = chenhLechColRange.ConditionalFormatting.AddGreaterThan();
                cfRuleGain.Formula = "0"; // Gán giá trị so sánh là "0"
                cfRuleGain.Style.Font.Color.Color = System.Drawing.Color.Green;

                ws2.Cells[ws2.Dimension.Address].AutoFitColumns();

                // --- 5. SHEET 4: LỊCH SỬ HỦY HÀNG ---
                var ws3 = package.Workbook.Worksheets.Add("Lịch Sử Hủy Hàng");
                ws3.Cells["A1:E1"].Value = "Lịch sử Hủy Hàng";
                styleMainTitle(ws3.Cells["A1:E1"]);

                ws3.Cells["A3"].LoadFromCollection(data.LichSuHuyHang, true, OfficeOpenXml.Table.TableStyles.Medium11);
                var tbl3 = ws3.Tables[0];

                // Định dạng cột ngày
                var ngayHuyColRange = ws3.Cells[tbl3.Address.Start.Row + 1, tbl3.Columns["NgayHuy"].Position + 1, tbl3.Address.End.Row, tbl3.Columns["NgayHuy"].Position + 1];
                ngayHuyColRange.Style.Numberformat.Format = dateFormat;

                // Định dạng cột số lượng hủy
                ws3.Cells[tbl3.Address.Start.Row + 1, tbl3.Columns["SoLuongHuy"].Position + 1, tbl3.Address.End.Row, tbl3.Columns["SoLuongHuy"].Position + 1].Style.Numberformat.Format = decimalFormat;

                // Định dạng cột giá trị hủy (tiền tệ)
                var giaTriHuyColRange = ws3.Cells[tbl3.Address.Start.Row + 1, tbl3.Columns["GiaTriHuy"].Position + 1, tbl3.Address.End.Row, tbl3.Columns["GiaTriHuy"].Position + 1];
                giaTriHuyColRange.Style.Numberformat.Format = currencyFormat;
                // Tô màu đỏ cho giá trị hủy
                var cfRuleHuy = giaTriHuyColRange.ConditionalFormatting.AddGreaterThan();
                cfRuleHuy.Formula = "0"; // Gán giá trị so sánh là "0"
                cfRuleHuy.Style.Font.Color.Color = System.Drawing.Color.Red;

                ws3.Cells[ws3.Dimension.Address].AutoFitColumns();

                // --- 6. LƯU FILE ---
                await package.SaveAsync();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}