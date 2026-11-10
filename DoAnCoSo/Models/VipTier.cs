namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model định nghĩa một cấp độ VIP trong chính sách tích lũy chiết khấu.
    /// </summary>
    public class VipTier
    {
        public int Level { get; set; }
        public string TenHang { get; set; } = string.Empty;
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int PhanTramGiam { get; set; }
        public string BadgeClass { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-gem";
        public string MauSac { get; set; } = "#4f46e5";
    }

    /// <summary>
    /// ViewModel chi tiết trạng thái VIP của khách hàng hoặc giỏ hàng
    /// </summary>
    public class VipStatusViewModel
    {
        public VipTier CurrentTier { get; set; } = new VipTier();
        public VipTier? NextTier { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal AmountToNextTier { get; set; }
        public int ProgressPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
