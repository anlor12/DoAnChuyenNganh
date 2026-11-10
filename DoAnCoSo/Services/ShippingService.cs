using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DoAnCoSo.Models;

namespace DoAnCoSo.Services
{
    public class ShippingCarrier
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Logo { get; set; } = string.Empty;
        public decimal BaseFee { get; set; }
        public string EstimatedTime { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Hotline { get; set; } = string.Empty;
        public string TrackingUrlTemplate { get; set; } = string.Empty;
    }

    public class TrackingEvent
    {
        public DateTime Time { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "completed"; // completed, current, pending
        public string Icon { get; set; } = "fa-check";
    }

    public class TrackingInfo
    {
        public string TrackingCode { get; set; } = string.Empty;
        public string CarrierCode { get; set; } = string.Empty;
        public string CarrierName { get; set; } = string.Empty;
        public string CarrierHotline { get; set; } = string.Empty;
        public string TrackingUrl { get; set; } = string.Empty;
        public string ShipperName { get; set; } = "Nguyễn Văn Tuấn";
        public string ShipperPhone { get; set; } = "0981.234.567";
        public string CurrentStatus { get; set; } = "Đang giao hàng";
        public string EstimatedDeliveryDate { get; set; } = string.Empty;
        public List<TrackingEvent> Events { get; set; } = new List<TrackingEvent>();
    }

    public static class ShippingService
    {
        public static readonly List<ShippingCarrier> Carriers = new List<ShippingCarrier>
        {
            new ShippingCarrier
            {
                Code = "GHTK",
                Name = "Giao Hàng Tiết Kiệm",
                Logo = "/images/carriers/ghtk.png",
                BaseFee = 25000,
                EstimatedTime = "2 - 3 ngày",
                Description = "Tiết kiệm, phủ sóng 63 tỉnh thành & huyện đảo",
                Hotline = "1900 6092",
                TrackingUrlTemplate = "https://i.ghtk.vn/{0}"
            },
            new ShippingCarrier
            {
                Code = "GHN",
                Name = "Giao Hàng Nhanh (Express)",
                Logo = "/images/carriers/ghn.png",
                BaseFee = 35000,
                EstimatedTime = "1 - 2 ngày",
                Description = "Hỏa tốc tối ưu thời gian, cam kết đúng giờ",
                Hotline = "1900 1201",
                TrackingUrlTemplate = "https://donhang.ghn.vn/?order_code={0}"
            },
            new ShippingCarrier
            {
                Code = "VTP",
                Name = "Viettel Post",
                Logo = "/images/carriers/vtp.png",
                BaseFee = 30000,
                EstimatedTime = "2 - 3 ngày",
                Description = "Mạng lưới bưu cục quân đội rộng khắp toàn quốc",
                Hotline = "1900 8095",
                TrackingUrlTemplate = "https://viettelpost.vn/tra-cuu-hanh-trinh-don/{0}"
            }
        };

        public static decimal FreeShippingThreshold = 500000m; // 500,000 ₫

        // 1. Dữ liệu địa giới hành chính mẫu (63 Tỉnh/Thành phố & Quận/Huyện/Xã trọng điểm)
        public static readonly List<string> Provinces = new List<string>
        {
            "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng", "Hải Phòng", "Cần Thơ",
            "Bình Dương", "Đồng Nai", "Quảng Ninh", "Bắc Ninh", "Hải Dương",
            "Thái Nguyên", "Nam Định", "Thái Bình", "Ninh Bình", "Thanh Hóa",
            "Nghệ An", "Hà Tĩnh", "Quảng Bình", "Quảng Trị", "Thừa Thiên Huế",
            "Quảng Nam", "Quảng Ngãi", "Bình Định", "Phú Yên", "Khánh Hòa",
            "Ninh Thuận", "Bình Thuận", "Lâm Đồng", "Đắk Lắk", "Gia Lai",
            "Bà Rịa - Vũng Tàu", "Tây Ninh", "Bình Phước", "Long An", "Tiền Giang",
            "Bến Tre", "Vĩnh Long", "Đồng Tháp", "An Giang", "Kiên Giang",
            "Hậu Giang", "Sóc Trăng", "Bạc Liêu", "Cà Mau", "Hòa Bình",
            "Sơn La", "Điện Biên", "Lai Châu", "Lào Cai", "Yên Bái",
            "Hà Giang", "Tuyên Quang", "Cao Bằng", "Bắc Kạn", "Lạng Sơn",
            "Bắc Giang", "Phú Thọ", "Vĩnh Phúc", "Hưng Yên", "Hà Nam",
            "Kon Tum", "Đắk Nông"
        };

        public static readonly Dictionary<string, List<string>> DistrictsByProvince = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Hà Nội"] = new List<string> { "Quận Ba Đình", "Quận Hoàn Kiếm", "Quận Đống Đa", "Quận Cầu Giấy", "Quận Hai Bà Trưng", "Quận Thanh Xuân", "Quận Hoàng Mai", "Quận Long Biên", "Quận Tây Hồ", "Quận Nam Từ Liêm", "Quận Bắc Từ Liêm", "Quận Hà Đông", "Huyện Thanh Trì", "Huyện Gia Lâm", "Huyện Đông Anh", "Huyện Sóc Sơn", "Huyện Hoài Đức" },
            ["TP. Hồ Chí Minh"] = new List<string> { "Quận 1", "Quận 3", "Quận 4", "Quận 5", "Quận 6", "Quận 7", "Quận 8", "Quận 10", "Quận 11", "Quận 12", "Thành phố Thủ Đức", "Quận Bình Thạnh", "Quận Gò Vấp", "Quận Tân Bình", "Quận Tân Phú", "Quận Phú Nhuận", "Quận Bình Tân", "Huyện Bình Chánh", "Huyện Hóc Môn", "Huyện Củ Chi", "Huyện Nhà Bè" },
            ["Đà Nẵng"] = new List<string> { "Quận Hải Châu", "Quận Thanh Khê", "Quận Sơn Trà", "Quận Ngũ Hành Sơn", "Quận Liên Chiểu", "Quận Cẩm Lệ", "Huyện Hòa Vang" },
            ["Hải Phòng"] = new List<string> { "Quận Hồng Bàng", "Quận Ngô Quyền", "Quận Lê Chân", "Quận Hải An", "Quận Kiến An", "Huyện Thủy Nguyên", "Huyện An Dương" },
            ["Cần Thơ"] = new List<string> { "Quận Ninh Kiều", "Quận Bình Thủy", "Quận Cái Răng", "Quận Ô Môn", "Quận Thốt Nốt" },
            ["Bình Dương"] = new List<string> { "TP. Thủ Dầu Một", "TP. Thuận An", "TP. Dĩ An", "TP. Tân Uyên", "Thị xã Bến Cát" },
            ["Đồng Nai"] = new List<string> { "TP. Biên Hòa", "TP. Long Khánh", "Huyện Long Thành", "Huyện Nhơn Trạch", "Huyện Trảng Bom" },
            ["Quảng Ninh"] = new List<string> { "TP. Hạ Long", "TP. Cẩm Phả", "TP. Uông Bí", "TP. Móng Cái" },
            ["Bắc Ninh"] = new List<string> { "TP. Bắc Ninh", "TP. Từ Sơn", "Thị xã Quế Võ", "Thị xã Thuận Thành", "Huyện Tiên Du", "Huyện Yên Phong" }
        };

        public static readonly Dictionary<string, List<string>> WardsByDistrict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Quận Ba Đình"] = new List<string> { "Phường Điện Biên", "Phường Đội Cấn", "Phường Kim Mã", "Phường Liễu Giai", "Phường Cống Vị", "Phường Quán Thánh" },
            ["Quận Hoàn Kiếm"] = new List<string> { "Phường Hàng Bạc", "Phường Tràng Tiền", "Phường Hàng Bài", "Phường Cửa Đông", "Phường Đồng Xuân" },
            ["Quận Đống Đa"] = new List<string> { "Phường Láng Hạ", "Phường Ô Chợ Dừa", "Phường Kim Liên", "Phường Cát Linh", "Phường Trung Tự" },
            ["Quận Cầu Giấy"] = new List<string> { "Phường Dịch Vọng", "Phường Dịch Vọng Hậu", "Phường Quan Hoa", "Phường Nghĩa Đô", "Phường Yên Hòa", "Phường Trung Hòa" },
            ["Quận 1"] = new List<string> { "Phường Bến Nghé", "Phường Bến Thành", "Phường Đa Kao", "Phường Tân Định", "Phường Phạm Ngũ Lão" },
            ["Quận 3"] = new List<string> { "Phường Võ Thị Sáu", "Phường 1", "Phường 2", "Phường 3", "Phường 4", "Phường 9" },
            ["Thành phố Thủ Đức"] = new List<string> { "Phường Thảo Điền", "Phường An Phú", "Phường Thủ Thiêm", "Phường Linh Trung", "Phường Tăng Nhơn Phú A" },
            ["Quận Hải Châu"] = new List<string> { "Phường Hải Châu 1", "Phường Hải Châu 2", "Phường Thạch Thang", "Phường Thanh Bình", "Phường Hòa Cường Bắc" }
        };

        public static List<string> GetDistricts(string province)
        {
            if (DistrictsByProvince.TryGetValue(province, out var list)) return list;
            return new List<string> { "Quận / Huyện Trung Tâm", "Quận / Huyện Ngoại Thành 1", "Quận / Huyện Ngoại Thành 2" };
        }

        public static List<string> GetWards(string district)
        {
            if (WardsByDistrict.TryGetValue(district, out var list)) return list;
            return new List<string> { "Phường / Xã 01", "Phường / Xã 02", "Phường / Xã Trung Tâm" };
        }

        /// <summary>
        /// Tính toán phí vận chuyển thực tế đa cấp (Đơn vị + Tỉnh/Thành + Giá trị đơn + VIP)
        /// </summary>
        public static (decimal Fee, bool IsFree, string Reason, string EstimatedTime, string EstimatedDate) CalculateShippingFee(
            string carrierCode, 
            string province, 
            string? district = null, 
            decimal orderSubtotal = 0, 
            int vipTierLevel = 1)
        {
            var carrier = Carriers.Find(c => c.Code.Equals(carrierCode, StringComparison.OrdinalIgnoreCase)) ?? Carriers[0];

            // 1. Miễn phí cho đơn hàng từ 500.000₫ trở lên
            if (orderSubtotal >= FreeShippingThreshold)
            {
                var estDays = carrier.Code == "GHN" ? 1 : 2;
                var arrivalDate = DateTime.Now.AddDays(estDays).ToString("dd/MM/yyyy");
                return (0, true, "Đơn hàng từ 500.000₫ được Miễn phí vận chuyển (Freeship)!", carrier.EstimatedTime, arrivalDate);
            }

            // 2. Miễn phí cho thành viên VIP cao cấp
            if (vipTierLevel >= 5)
            {
                var estDays = carrier.Code == "GHN" ? 1 : 2;
                var arrivalDate = DateTime.Now.AddDays(estDays).ToString("dd/MM/yyyy");
                return (0, true, "Đặc quyền Hội viên VIP cao cấp: Miễn phí vận chuyển toàn quốc!", carrier.EstimatedTime, arrivalDate);
            }

            // 3. Tính phí theo vùng địa lý
            decimal baseFee = carrier.BaseFee;
            string estTime = carrier.EstimatedTime;
            int daysToAdd = 2;

            bool isHanoiOrHcm = !string.IsNullOrEmpty(province) && 
                (province.Contains("Hà Nội", StringComparison.OrdinalIgnoreCase) || province.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase));

            bool isTier1 = !string.IsNullOrEmpty(province) &&
                (province.Contains("Đà Nẵng") || province.Contains("Hải Phòng") || province.Contains("Bình Dương") || province.Contains("Đồng Nai") || province.Contains("Cần Thơ") || province.Contains("Quảng Ninh") || province.Contains("Bắc Ninh"));

            bool isRemote = !string.IsNullOrEmpty(province) &&
                (province.Contains("Hà Giang") || province.Contains("Điện Biên") || province.Contains("Lai Châu") || province.Contains("Cao Bằng") || province.Contains("Sơn La") || province.Contains("Kon Tum") || province.Contains("Đắk Nông"));

            if (isHanoiOrHcm)
            {
                baseFee = carrier.Code switch
                {
                    "GHTK" => 20000,
                    "GHN" => 25000,
                    "VTP" => 22000,
                    _ => 22000
                };
                estTime = carrier.Code == "GHN" ? "Hỏa tốc 4h - 24h" : "1 - 2 ngày";
                daysToAdd = carrier.Code == "GHN" ? 1 : 2;
            }
            else if (isTier1)
            {
                baseFee = carrier.Code switch
                {
                    "GHTK" => 28000,
                    "GHN" => 32000,
                    "VTP" => 29000,
                    _ => 28000
                };
                estTime = "2 ngày";
                daysToAdd = 2;
            }
            else if (isRemote)
            {
                baseFee = carrier.Code switch
                {
                    "GHTK" => 40000,
                    "GHN" => 45000,
                    "VTP" => 38000,
                    _ => 40000
                };
                estTime = "3 - 4 ngày";
                daysToAdd = 4;
            }
            else
            {
                baseFee = carrier.Code switch
                {
                    "GHTK" => 30000,
                    "GHN" => 35000,
                    "VTP" => 32000,
                    _ => 30000
                };
                estTime = "2 - 3 ngày";
                daysToAdd = 3;
            }

            var estArrivalDate = DateTime.Now.AddDays(daysToAdd).ToString("dd/MM/yyyy");
            return (baseFee, false, $"Cước vận chuyển {carrier.Name} tuyến {province ?? "toàn quốc"}", estTime, estArrivalDate);
        }

        /// <summary>
        /// Backward-compatible overload
        /// </summary>
        public static (decimal Fee, bool IsFree, string Reason) CalculateShippingFee(string carrierCode, decimal orderSubtotal, int vipTierLevel = 1)
        {
            var res = CalculateShippingFee(carrierCode, "Hà Nội", null, orderSubtotal, vipTierLevel);
            return (res.Fee, res.IsFree, res.Reason);
        }

        /// <summary>
        /// Tạo mã vận đơn chuẩn Logistics
        /// </summary>
        public static string GenerateTrackingCode(string carrierCode, int orderId)
        {
            var prefix = carrierCode.ToUpper();
            var randomPart = new Random().Next(100000, 999999);
            return $"{prefix}-{orderId:D4}{randomPart}";
        }

        /// <summary>
        /// Trích xuất hoặc giả lập hành trình bưu kiện (Order Tracking timeline)
        /// </summary>
        public static TrackingInfo GetOrderTracking(DonHang order)
        {
            var carrier = Carriers.Find(c => c.Code.Equals(order.DonViVanChuyen, StringComparison.OrdinalIgnoreCase)) ?? Carriers[0];
            var trackingCode = !string.IsNullOrEmpty(order.MaVanDon) 
                ? order.MaVanDon 
                : GenerateTrackingCode(carrier.Code, order.MaDonHang);

            var info = new TrackingInfo
            {
                TrackingCode = trackingCode,
                CarrierCode = carrier.Code,
                CarrierName = carrier.Name,
                CarrierHotline = carrier.Hotline,
                TrackingUrl = string.Format(carrier.TrackingUrlTemplate, trackingCode),
                ShipperName = !string.IsNullOrEmpty(order.NhanVienGiaoHang) ? order.NhanVienGiaoHang : "Nguyễn Văn Tuấn (Shipper phụ trách)",
                ShipperPhone = "0981.234.567",
                CurrentStatus = order.TrangThaiDonHang,
                EstimatedDeliveryDate = order.NgayDat.AddDays(2).ToString("dd/MM/yyyy")
            };

            var orderDate = order.NgayDat;

            // Xây dựng các mốc theo trạng thái đơn hàng
            var events = new List<TrackingEvent>();

            // Mốc 1: Đặt hàng thành công
            events.Add(new TrackingEvent
            {
                Time = orderDate,
                Title = "Tiếp nhận đơn hàng thành công",
                Location = "Hệ thống Fashion Store Online",
                Description = $"Đơn hàng #{order.MaDonHang} đã được tạo thành công với phương thức thanh toán {order.PhuongThucThanhToan}.",
                Status = "completed",
                Icon = "fa-check-circle"
            });

            // Mốc 2: Kho xác nhận & Đóng gói
            bool isProcessingOrBeyond = order.TrangThaiDonHang == "Đang xử lý" || order.TrangThaiDonHang == "Đang giao" || order.TrangThaiDonHang == "Hoàn thành";
            events.Add(new TrackingEvent
            {
                Time = orderDate.AddMinutes(45),
                Title = "Đóng gói hoàn tất & In phiếu gửi",
                Location = "Kho Tổng Fashion Store Logistics - Hà Nội",
                Description = "Nhân viên kho đã nhặt hàng theo Pick list, đóng hộp carton niêm phong và dán tem vận chuyển.",
                Status = isProcessingOrBeyond ? "completed" : (order.TrangThaiDonHang == "Chờ xác nhận" ? "current" : "pending"),
                Icon = "fa-box"
            });

            // Mốc 3: Bàn giao bưu cục vận chuyển
            bool isShippingOrBeyond = order.TrangThaiDonHang == "Đang giao" || order.TrangThaiDonHang == "Hoàn thành";
            events.Add(new TrackingEvent
            {
                Time = orderDate.AddHours(3),
                Title = $"Bàn giao cho đơn vị vận chuyển ({carrier.Name})",
                Location = $"Bưu cục trung chuyển {carrier.Code} - Đống Đa, Hà Nội",
                Description = $"Kiện hàng đã được bàn giao cho đối tác {carrier.Name}. Mã vận đơn theo dõi: {trackingCode}.",
                Status = isShippingOrBeyond ? "completed" : (order.TrangThaiDonHang == "Đang xử lý" ? "current" : "pending"),
                Icon = "fa-truck-loading"
            });

            // Mốc 4: Đang trung chuyển & Bưu tá đi phát
            events.Add(new TrackingEvent
            {
                Time = orderDate.AddDays(1).AddHours(4),
                Title = "Bưu tá đang tiến hành giao hàng",
                Location = $"Bưu cục phát gần bạn: {order.TinhThanh ?? "Địa bàn người nhận"}",
                Description = $"Bưu tá {info.ShipperName} (SĐT: {info.ShipperPhone}) đang liên hệ giao hàng tới {order.DiaChiGiaoHang}.",
                Status = order.TrangThaiDonHang == "Hoàn thành" ? "completed" : (order.TrangThaiDonHang == "Đang giao" ? "current" : "pending"),
                Icon = "fa-shipping-fast"
            });

            // Mốc 5: Giao hàng thành công
            bool isCompleted = order.TrangThaiDonHang == "Hoàn thành";
            events.Add(new TrackingEvent
            {
                Time = isCompleted ? orderDate.AddDays(2).AddHours(1) : orderDate.AddDays(2),
                Title = isCompleted ? "Giao hàng thành công" : "Dự kiến giao hàng thành công",
                Location = order.DiaChiGiaoHang,
                Description = isCompleted 
                    ? "Kiện hàng đã được người nhận ký nhận nguyên vẹn. Đơn hàng hoàn tất." 
                    : $"Kiện hàng dự kiến giao tới bạn trước 18:00 ngày {info.EstimatedDeliveryDate}.",
                Status = isCompleted ? "completed" : "pending",
                Icon = "fa-clipboard-check"
            });

            info.Events = events;
            return info;
        }

        /// <summary>
        /// Tạo chuỗi mã vạch Code 128 dạng SVG độc lập để in Waybill
        /// </summary>
        public static string GenerateBarcodeSvg(string text, int height = 50)
        {
            if (string.IsNullOrEmpty(text)) text = "FS-PARCEL";
            var rand = new Random(text.GetHashCode());
            
            // Tạo chuỗi vạch SVG giả lập Code 128 chuẩn tỷ lệ hình học
            var svgBars = new System.Text.StringBuilder();
            int currentX = 10;
            svgBars.Append(@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 320 60"" width=""100%"" height=""100%"">");
            svgBars.Append(@"<rect width=""320"" height=""60"" fill=""#ffffff""/>");

            // Guard bars
            svgBars.Append($@"<rect x=""{currentX}"" y=""5"" width=""2"" height=""{height}"" fill=""#000000""/>");
            currentX += 4;
            svgBars.Append($@"<rect x=""{currentX}"" y=""5"" width=""2"" height=""{height}"" fill=""#000000""/>");
            currentX += 5;

            // Data bars
            for (int i = 0; i < text.Length; i++)
            {
                int val = (int)text[i];
                int w1 = (val % 3) + 1;
                int w2 = ((val / 3) % 3) + 1;
                int gap = ((val / 9) % 2) + 2;

                svgBars.Append($@"<rect x=""{currentX}"" y=""5"" width=""{w1}"" height=""{height}"" fill=""#000000""/>");
                currentX += w1 + gap;
                svgBars.Append($@"<rect x=""{currentX}"" y=""5"" width=""{w2}"" height=""{height}"" fill=""#000000""/>");
                currentX += w2 + gap;

                if (currentX > 290) break;
            }

            // Stop bars
            svgBars.Append($@"<rect x=""{currentX}"" y=""5"" width=""3"" height=""{height}"" fill=""#000000""/>");
            currentX += 5;
            svgBars.Append($@"<rect x=""{currentX}"" y=""5"" width=""2"" height=""{height}"" fill=""#000000""/>");

            svgBars.Append("</svg>");
            return svgBars.ToString();
        }
    }
}
