using DoAnCoSo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DoAnCoSo.Services
{
    /// <summary>
    /// Service xử lý logic Hạng VIP và Chiết khấu tự động theo bảng nấc thang giá trị.
    /// </summary>
    public static class VipService
    {
        private static readonly List<VipTier> Tiers = new List<VipTier>
        {
            new VipTier
            {
                Level = 1,
                TenHang = "VIP Khởi Đầu",
                MinAmount = 0,
                MaxAmount = 999999,
                PhanTramGiam = 5,
                BadgeClass = "bg-secondary text-white",
                Icon = "bi-person-check-fill",
                MauSac = "#64748b"
            },
            new VipTier
            {
                Level = 2,
                TenHang = "VIP Bạc",
                MinAmount = 1000000,
                MaxAmount = 1999999,
                PhanTramGiam = 6,
                BadgeClass = "bg-info text-dark",
                Icon = "bi-shield-fill-check",
                MauSac = "#0284c7"
            },
            new VipTier
            {
                Level = 3,
                TenHang = "VIP Vàng",
                MinAmount = 2000000,
                MaxAmount = 2999999,
                PhanTramGiam = 7,
                BadgeClass = "bg-warning text-dark",
                Icon = "bi-star-fill",
                MauSac = "#d97706"
            },
            new VipTier
            {
                Level = 4,
                TenHang = "VIP Titan",
                MinAmount = 3000000,
                MaxAmount = 3999999,
                PhanTramGiam = 8,
                BadgeClass = "bg-primary text-white",
                Icon = "bi-award-fill",
                MauSac = "#2563eb"
            },
            new VipTier
            {
                Level = 5,
                TenHang = "VIP Bạch Kim",
                MinAmount = 4000000,
                MaxAmount = 4999999,
                PhanTramGiam = 9,
                BadgeClass = "bg-dark text-white",
                Icon = "bi-trophy-fill",
                MauSac = "#0f172a"
            },
            new VipTier
            {
                Level = 6,
                TenHang = "VIP Ruby",
                MinAmount = 5000000,
                MaxAmount = 5999999,
                PhanTramGiam = 10,
                BadgeClass = "bg-danger text-white",
                Icon = "bi-gem",
                MauSac = "#e11d48"
            },
            new VipTier
            {
                Level = 7,
                TenHang = "VIP Sapphire",
                MinAmount = 6000000,
                MaxAmount = 6999999,
                PhanTramGiam = 11,
                BadgeClass = "bg-primary text-white",
                Icon = "bi-diamond-fill",
                MauSac = "#0284c7"
            },
            new VipTier
            {
                Level = 8,
                TenHang = "VIP Emerald",
                MinAmount = 7000000,
                MaxAmount = 7999999,
                PhanTramGiam = 12,
                BadgeClass = "bg-success text-white",
                Icon = "bi-patch-check-fill",
                MauSac = "#059669"
            },
            new VipTier
            {
                Level = 9,
                TenHang = "VIP Kim Cương",
                MinAmount = 8000000,
                MaxAmount = 8999999,
                PhanTramGiam = 13,
                BadgeClass = "bg-info text-white",
                Icon = "bi-gem",
                MauSac = "#0891b2"
            },
            new VipTier
            {
                Level = 10,
                TenHang = "VIP Tinh Anh",
                MinAmount = 9000000,
                MaxAmount = 9999999,
                PhanTramGiam = 14,
                BadgeClass = "bg-dark text-warning",
                Icon = "bi-stars",
                MauSac = "#7c3aed"
            },
            new VipTier
            {
                Level = 11,
                TenHang = "VIP Hoàng Gia",
                MinAmount = 10000000,
                MaxAmount = decimal.MaxValue,
                PhanTramGiam = 15,
                BadgeClass = "bg-danger text-warning",
                Icon = "bi-crown-fill",
                MauSac = "#dc2626"
            }
        };

        /// <summary>
        /// Lấy toàn bộ danh sách 11 mốc VIP
        /// </summary>
        public static List<VipTier> GetAllTiers() => Tiers;

        /// <summary>
        /// Xác định hạng VIP theo số tiền chi tiêu / giá trị đơn hàng
        /// </summary>
        public static VipTier GetTierByAmount(decimal amount)
        {
            if (amount < 0) amount = 0;
            return Tiers.FirstOrDefault(t => amount >= t.MinAmount && amount <= t.MaxAmount) ?? Tiers.Last();
        }

        /// <summary>
        /// Lấy thông tin trạng thái VIP, hạng kế tiếp và tiến trình nâng hạng
        /// </summary>
        public static VipStatusViewModel GetVipStatus(decimal amount)
        {
            if (amount < 0) amount = 0;
            var currentTier = GetTierByAmount(amount);
            var nextTier = Tiers.FirstOrDefault(t => t.Level == currentTier.Level + 1);

            decimal amountToNext = 0;
            int progress = 100;

            if (nextTier != null)
            {
                amountToNext = nextTier.MinAmount - amount;
                if (amountToNext < 0) amountToNext = 0;

                decimal range = nextTier.MinAmount - currentTier.MinAmount;
                if (range > 0)
                {
                    decimal currentProgress = amount - currentTier.MinAmount;
                    progress = (int)Math.Clamp((currentProgress / range) * 100, 0, 100);
                }
            }

            return new VipStatusViewModel
            {
                CurrentTier = currentTier,
                NextTier = nextTier,
                CurrentAmount = amount,
                AmountToNextTier = amountToNext,
                ProgressPercent = progress,
                DiscountAmount = 0
            };
        }

        /// <summary>
        /// Tính toán chiết khấu VIP áp dụng cho đơn hàng:
        /// So sánh giữa Giá trị đơn hàng hiện tại và Tổng chi tiêu tích lũy của khách hàng,
        /// chọn mức có lợi nhất (phần trăm giảm cao nhất) cho khách hàng!
        /// </summary>
        public static (VipTier AppliedTier, decimal DiscountAmount, string Description) CalculateVipDiscount(decimal orderSubtotal, decimal customerAccumulatedSpending = 0)
        {
            if (orderSubtotal <= 0)
            {
                return (Tiers[0], 0, "Đơn hàng trống");
            }

            var orderTier = GetTierByAmount(orderSubtotal);
            var customerTier = GetTierByAmount(customerAccumulatedSpending);

            // Chọn mức chiết khấu cao nhất
            var chosenTier = (orderTier.PhanTramGiam >= customerTier.PhanTramGiam) ? orderTier : customerTier;
            string reason = (chosenTier.Level == orderTier.Level) 
                ? $"Đơn hàng đạt {orderSubtotal:N0}₫ ({chosenTier.TenHang})" 
                : $"Tài khoản VIP tích lũy {customerAccumulatedSpending:N0}₫ ({chosenTier.TenHang})";

            decimal discount = Math.Round(orderSubtotal * chosenTier.PhanTramGiam / 100m, 0);

            return (chosenTier, discount, reason);
        }
    }
}
