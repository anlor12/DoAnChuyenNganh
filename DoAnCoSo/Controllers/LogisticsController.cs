using System;
using System.Threading.Tasks;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using DoAnCoSo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogisticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LogisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Provinces")]
        public IActionResult GetProvinces()
        {
            return Ok(ShippingService.Provinces);
        }

        [HttpGet("Districts")]
        public IActionResult GetDistricts([FromQuery] string province)
        {
            if (string.IsNullOrEmpty(province)) return BadRequest("Province required");
            return Ok(ShippingService.GetDistricts(province));
        }

        [HttpGet("Wards")]
        public IActionResult GetWards([FromQuery] string district)
        {
            if (string.IsNullOrEmpty(district)) return BadRequest("District required");
            return Ok(ShippingService.GetWards(district));
        }

        public class FeeCalculationRequest
        {
            public string CarrierCode { get; set; } = "GHTK";
            public string Province { get; set; } = "Hà Nội";
            public string? District { get; set; }
            public decimal Subtotal { get; set; }
            public int VipLevel { get; set; } = 1;
        }

        [HttpPost("CalculateFee")]
        public IActionResult CalculateFee([FromBody] FeeCalculationRequest req)
        {
            var res = ShippingService.CalculateShippingFee(
                req.CarrierCode, 
                req.Province, 
                req.District, 
                req.Subtotal, 
                req.VipLevel);

            return Ok(new
            {
                success = true,
                fee = res.Fee,
                isFree = res.IsFree,
                reason = res.Reason,
                estimatedTime = res.EstimatedTime,
                estimatedDate = res.EstimatedDate
            });
        }

        [HttpPost("DispatchOrder/{orderId}")]
        public async Task<IActionResult> DispatchOrder(int orderId, [FromQuery] string? carrierCode)
        {
            var order = await _context.DonHangs.FindAsync(orderId);
            if (order == null) return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

            if (!string.IsNullOrEmpty(carrierCode))
            {
                order.DonViVanChuyen = carrierCode;
            }
            if (string.IsNullOrEmpty(order.DonViVanChuyen))
            {
                order.DonViVanChuyen = "GHTK";
            }

            order.MaVanDon = ShippingService.GenerateTrackingCode(order.DonViVanChuyen, order.MaDonHang);
            order.TrangThaiDonHang = "Đang giao";
            order.NhanVienGiaoHang = "Nguyễn Văn Tuấn (0981.234.567)";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Đã tạo mã vận đơn {order.MaVanDon} và đẩy đơn sang nhà xe {order.DonViVanChuyen} thành công!",
                trackingCode = order.MaVanDon,
                carrier = order.DonViVanChuyen
            });
        }
    }
}
