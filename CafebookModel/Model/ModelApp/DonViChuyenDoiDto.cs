namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO cho DataGrid
    /// </summary>
    public class DonViChuyenDoiDtoo
    {
        public int IdChuyenDoi { get; set; }
        public int IdNguyenLieu { get; set; }
        public string TenNguyenLieu { get; set; } = string.Empty;
        public string TenDonVi { get; set; } = string.Empty;
        public decimal GiaTriQuyDoi { get; set; }
        public bool LaDonViCoBan { get; set; }
    }

    /// <summary>
    /// DTO để Gửi (Thêm/Sửa)
    /// </summary>
    public class DonViChuyenDoiUpdateRequestDto
    {
        public int IdNguyenLieu { get; set; }
        public string TenDonVi { get; set; } = string.Empty;
        public decimal GiaTriQuyDoi { get; set; }
        public bool LaDonViCoBan { get; set; }
    }
}