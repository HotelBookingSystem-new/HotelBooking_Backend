using Microsoft.EntityFrameworkCore;
using Backend.DTOs;
using Backend.Data;

namespace Backend.Services
{
    public class PaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(ApplicationDbContext context, ILogger<PaymentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(int bookingId, PaymentDetailsDto paymentDetails, decimal amount)
        {
            try
            {
                // Simulate payment gateway integration
                var transactionId = GenerateTransactionId();
                var paymentStatus = "Success";

                // In real implementation, integrate with payment gateway like Stripe, PayPal, etc.
                var isValid = await ValidatePaymentDetails(paymentDetails, amount);

                if (!isValid)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Invalid payment details",
                        TransactionId = null
                    };
                }

                var payment = new Payment
                {
                    BookingId = bookingId,
                    Amount = amount,
                    PaymentMethod = paymentDetails.PaymentMethod,
                    TransactionId = transactionId,
                    PaymentStatus = paymentStatus,
                    PaymentDate = DateTime.UtcNow,
                    CardLastFourDigits = !string.IsNullOrEmpty(paymentDetails.CardNumber) && paymentDetails.CardNumber.Length >= 4
                        ? paymentDetails.CardNumber.Substring(paymentDetails.CardNumber.Length - 4)
                        : null,
                    BillingAddress = paymentDetails.BillingAddress
                };

                await _context.Payments.AddAsync(payment);
                await _context.SaveChangesAsync();

                return new PaymentResult
                {
                    Success = true,
                    Message = "Payment processed successfully",
                    TransactionId = transactionId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                return new PaymentResult
                {
                    Success = false,
                    Message = "Payment processing failed",
                    TransactionId = null
                };
            }
        }

        public async Task<PaymentResult> RefundPaymentAsync(int paymentId, decimal amount)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(paymentId);
                if (payment == null)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Payment not found",
                        TransactionId = null
                    };
                }

                // Simulate refund logic
                payment.PaymentStatus = "Refunded";
                await _context.SaveChangesAsync();

                return new PaymentResult
                {
                    Success = true,
                    Message = $"Refund of {amount:C} processed successfully",
                    TransactionId = payment.TransactionId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund");
                return new PaymentResult
                {
                    Success = false,
                    Message = "Refund processing failed",
                    TransactionId = null
                };
            }
        }

        public async Task<PaymentDto> GetPaymentByBookingIdAsync(int bookingId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == bookingId);

            if (payment == null)
                return null;

            return new PaymentDto
            {
                Id = payment.Id,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                TransactionId = payment.TransactionId,
                PaymentStatus = payment.PaymentStatus,
                PaymentDate = payment.PaymentDate,
                CardLastFourDigits = payment.CardLastFourDigits
            };
        }

        private async Task<bool> ValidatePaymentDetails(PaymentDetailsDto paymentDetails, decimal amount)
        {
            // Simulate payment validation
            await Task.Delay(100); // Simulate async operation

            switch (paymentDetails.PaymentMethod.ToLower())
            {
                case "creditcard":
                case "debitcard":
                    return ValidateCardDetails(paymentDetails);

                case "paypal":
                    return !string.IsNullOrEmpty(paymentDetails.Email);

                case "upi":
                    return !string.IsNullOrEmpty(paymentDetails.UpiId);

                default:
                    return false;
            }
        }

        private bool ValidateCardDetails(PaymentDetailsDto paymentDetails)
        {
            if (string.IsNullOrEmpty(paymentDetails.CardNumber) || paymentDetails.CardNumber.Length < 13)
                return false;

            if (string.IsNullOrEmpty(paymentDetails.CardHolderName))
                return false;

            if (string.IsNullOrEmpty(paymentDetails.ExpiryDate))
                return false;

            if (string.IsNullOrEmpty(paymentDetails.CVV) || paymentDetails.CVV.Length < 3)
                return false;

            // Validate expiry date
            if (DateTime.TryParse(paymentDetails.ExpiryDate, out DateTime expiryDate))
            {
                if (expiryDate < DateTime.UtcNow)
                    return false;
            }

            return true;
        }

        private string GenerateTransactionId()
        {
            return "TXN" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
        }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string TransactionId { get; set; }
    }
}