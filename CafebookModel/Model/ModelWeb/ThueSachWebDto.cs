using System;
using System.ComponentModel.DataAnnotations;

namespace CafebookModel.Model.ModelWeb
{
    /// <summary>
    /// DTO gửi từ web client (trang ThueSachView) lên API
    /// </summary>
    public class ThueSachRequestDto
    {
        [Required]
        public int IdSach { get; set; }
    }

    /// <summary>
    /// DTO API trả về sau khi khách hàng thuê thành công
    /// </summary>
    public class ThueSachResponseDto
    {
        public int IdPhieuThueSach { get; set; }
        public string TenSach { get; set; } = string.Empty;
        public DateTime NgayThue { get; set; }
        public DateTime NgayHenTra { get; set; }
        public decimal TongTienCoc { get; set; }
    }
}