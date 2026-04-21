using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Backend.DTOs;

namespace Backend.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration)
        {
            _smtpServer = configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
            _smtpUsername = configuration["Email:Username"] ?? throw new Exception("Email username not configured");
            _smtpPassword = configuration["Email:Password"] ?? throw new Exception("Email password not configured");
            _fromEmail = configuration["Email:FromEmail"] ?? _smtpUsername;
            _fromName = configuration["Email:FromName"] ?? "Luxury Hotel Booking";
        }

        // Core send method
        public async Task<bool> SendEmailAsync(EmailRequestDto request)
        {
            try
            {
                var subject = GetSubject(request.Type);
                var body = GetEmailBody(request.Type, request.Data);

                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(new MailAddress(request.ToEmail, request.ToName));

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // Log exception here (use ILogger in real app)
                Console.WriteLine($"Email sending failed: {ex.Message}");
                return false;
            }
        }

        // Convenience methods
        public async Task<bool> SendBookingConfirmationAsync(string email, string name, BookingEmailData data)
        {
            return await SendEmailAsync(new EmailRequestDto
            {
                ToEmail = email,
                ToName = name,
                Type = EmailType.BookingConfirmation,
                Data = data
            });
        }

        public async Task<bool> SendBookingCancellationAsync(string email, string name, CancellationEmailData data)
        {
            return await SendEmailAsync(new EmailRequestDto
            {
                ToEmail = email,
                ToName = name,
                Type = EmailType.BookingCancellation,
                Data = data
            });
        }

        public async Task<bool> SendRegistrationWelcomeAsync(string email, string name, RegistrationEmailData data)
        {
            return await SendEmailAsync(new EmailRequestDto
            {
                ToEmail = email,
                ToName = name,
                Type = EmailType.RegistrationWelcome,
                Data = data
            });
        }

        public async Task<bool> SendRebookingConfirmationAsync(string email, string name, RebookingEmailData data)
        {
            return await SendEmailAsync(new EmailRequestDto
            {
                ToEmail = email,
                ToName = name,
                Type = EmailType.RebookingConfirmation,
                Data = data
            });
        }

        public async Task<bool> SendPaymentConfirmationAsync(string email, string name, PaymentEmailData data)
        {
            return await SendEmailAsync(new EmailRequestDto
            {
                ToEmail = email,
                ToName = name,
                Type = EmailType.PaymentConfirmation,
                Data = data
            });
        }

        public async Task<bool> SendResendConfirmationAsync(string email, string name, ResendConfirmationData data)
        {
            return await SendEmailAsync(new EmailRequestDto
            {
                ToEmail = email,
                ToName = name,
                Type = EmailType.ResendConfirmation,
                Data = data
            });
        }

        // Private helpers
        private string GetSubject(EmailType type)
        {
            return type switch
            {
                EmailType.BookingConfirmation => "Booking Confirmation – Your Stay is Reserved",
                EmailType.BookingCancellation => "Booking Cancellation Confirmation",
                EmailType.RegistrationWelcome => "Welcome to Luxury Hotel Booking – Start Your Journey",
                EmailType.RebookingConfirmation => "Your Rebooking is Confirmed",
                EmailType.PaymentConfirmation => "Payment Received – Booking Secured",
                EmailType.ResendConfirmation => "Your Booking Details (Resent)",
                _ => "Message from Luxury Hotel Booking"
            };
        }

        private string GetEmailBody(EmailType type, dynamic data)
        {
            return type switch
            {
                EmailType.BookingConfirmation => BuildBookingConfirmationEmail(data),
                EmailType.BookingCancellation => BuildCancellationEmail(data),
                EmailType.RegistrationWelcome => BuildRegistrationEmail(data),
                EmailType.RebookingConfirmation => BuildRebookingEmail(data),
                EmailType.PaymentConfirmation => BuildPaymentEmail(data),
                EmailType.ResendConfirmation => BuildResendConfirmationEmail(data),
                _ => "<p>Thank you for using our service.</p>"
            };
        }

        private string BuildBookingConfirmationEmail(BookingEmailData data)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Booking Confirmation</title>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
                    .header {{ background: #1a3e60; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                    .content {{ padding: 20px; }}
                    .booking-details {{ background: #f5f5f5; padding: 15px; border-radius: 8px; margin: 15px 0; }}
                    .footer {{ text-align: center; font-size: 12px; color: #777; margin-top: 20px; border-top: 1px solid #ddd; padding-top: 10px; }}
                    .button {{ display: inline-block; background: #1a3e60; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; }}
                    h2 {{ color: #1a3e60; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Booking Confirmed! 🎉</h1>
                    </div>
                    <div class='content'>
                        <p>Dear Guest,</p>
                        <p>Your booking at <strong>{data.HotelName}</strong> has been confirmed.</p>
                        
                        <div class='booking-details'>
                            <h3>Booking Details</h3>
                            <p><strong>Booking Reference:</strong> {data.BookingReference}</p>
                            <p><strong>Room:</strong> {data.RoomCategory} - {data.RoomNumber}</p>
                            <p><strong>Check-in:</strong> {data.CheckInDate:dddd, MMMM dd, yyyy} (from 2:00 PM)</p>
                            <p><strong>Check-out:</strong> {data.CheckOutDate:dddd, MMMM dd, yyyy} (until 11:00 AM)</p>
                            <p><strong>Guests:</strong> {data.NumberOfGuests}</p>
                            <p><strong>Total Price:</strong> ${data.FinalPrice:N2} {(data.TotalPrice != data.FinalPrice ? $"(original ${data.TotalPrice:N2} after discount)" : "")}</p>
                            <p><strong>Loyalty Points Earned:</strong> {data.LoyaltyPointsEarned} points</p>
                            {(!string.IsNullOrEmpty(data.SpecialRequests) ? $"<p><strong>Special Requests:</strong> {data.SpecialRequests}</p>" : "")}
                        </div>
                        
                        <p>You can view or manage your booking anytime from your account dashboard.</p>
                        <p style='text-align: center;'>
                            <a href='https://yourdomain.com/bookings/{data.BookingReference}' class='button'>View Booking</a>
                        </p>
                        <p>Need help? Contact us at <a href='mailto:support@luxuryhotelbooking.com'>support@luxuryhotelbooking.com</a></p>
                    </div>
                    <div class='footer'>
                        <p>&copy; {DateTime.UtcNow.Year} Luxury Hotel Booking. All rights reserved.</p>
                        <p>This is a transactional email – please keep it for your records.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private string BuildCancellationEmail(CancellationEmailData data)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Booking Cancellation</title>
                <style>
                    body {{ font-family: Arial, sans-serif; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
                    .header {{ background: #d9534f; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                    .content {{ padding: 20px; }}
                    .details {{ background: #f9f9f9; padding: 15px; border-radius: 8px; margin: 15px 0; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Booking Cancelled</h1>
                    </div>
                    <div class='content'>
                        <p>Dear Guest,</p>
                        <p>Your booking <strong>{data.BookingReference}</strong> at <strong>{data.HotelName}</strong> has been successfully cancelled.</p>
                        <div class='details'>
                            <p><strong>Cancelled Dates:</strong> {data.CheckInDate:MMMM dd, yyyy} – {data.CheckOutDate:MMMM dd, yyyy}</p>
                            <p><strong>Refund Amount:</strong> ${data.RefundAmount:N2}</p>
                            {(!string.IsNullOrEmpty(data.CancellationReason) ? $"<p><strong>Reason:</strong> {data.CancellationReason}</p>" : "")}
                        </div>
                        <p>The refund will be processed to your original payment method within 5–7 business days.</p>
                        <p>We hope to welcome you again soon!</p>
                    </div>
                    <div class='footer' style='text-align:center; font-size:12px; color:#777; margin-top:20px;'>
                        <p>Luxury Hotel Booking – Your trusted travel partner</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private string BuildRegistrationEmail(RegistrationEmailData data)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Welcome to Luxury Hotel Booking</title>
                <style>
                    body {{ font-family: Arial, sans-serif; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
                    .header {{ background: #5cb85c; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                    .content {{ padding: 20px; }}
                    .loyalty-card {{ background: #f0f8ff; padding: 15px; border-radius: 8px; margin: 15px 0; text-align: center; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Welcome, {data.FullName}! 🌟</h1>
                    </div>
                    <div class='content'>
                        <p>Thank you for joining <strong>Luxury Hotel Booking</strong>. Your account has been successfully created.</p>
                        <div class='loyalty-card'>
                            <h3>Your Loyalty Program Status</h3>
                            <p><strong>Tier:</strong> {data.LoyaltyTier}</p>
                            <p><strong>Starting Points:</strong> {data.LoyaltyPoints} points</p>
                            <p>Earn 10 points for every $1 spent. Redeem points for discounts and free nights!</p>
                        </div>
                        <p>What you can do next:</p>
                        <ul>
                            <li>Browse 5,000+ hotels worldwide</li>
                            <li>Get exclusive member-only deals</li>
                            <li>Manage bookings and track loyalty points</li>
                        </ul>
                        <p style='text-align: center;'>
                            <a href='https://yourdomain.com' style='background:#5cb85c; color:white; padding:10px 20px; text-decoration:none; border-radius:5px;'>Start Exploring</a>
                        </p>
                    </div>
                    <div class='footer' style='text-align:center; font-size:12px; color:#777;'>
                        <p>You're receiving this email because you registered on our platform.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private string BuildRebookingEmail(RebookingEmailData data)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Rebooking Confirmed</title>
                <style>
                    body {{ font-family: Arial, sans-serif; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
                    .header {{ background: #f0ad4e; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Rebooking Successful!</h1>
                    </div>
                    <div class='content'>
                        <p>Dear Guest,</p>
                        <p>You have successfully rebooked your stay at <strong>{data.HotelName}</strong>.</p>
                        <p><strong>New Booking Reference:</strong> {data.NewBookingReference}</p>
                        <p><strong>Previous Reference:</strong> {data.OldBookingReference}</p>
                        <p><strong>New Dates:</strong> {data.CheckInDate:MMMM dd, yyyy} – {data.CheckOutDate:MMMM dd, yyyy}</p>
                        <p><strong>Total Price:</strong> ${data.NewPrice:N2}</p>
                        <p>A confirmation email with full details has been sent. You can view all your bookings in your account.</p>
                        <p>Thank you for choosing us again!</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private string BuildPaymentEmail(PaymentEmailData data)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Payment Received</title>
                <style>
                    body {{ font-family: Arial, sans-serif; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
                    .header {{ background: #5bc0de; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Payment Confirmed ✅</h1>
                    </div>
                    <div class='content'>
                        <p>Dear Guest,</p>
                        <p>We have successfully received your payment for booking <strong>{data.BookingReference}</strong> at <strong>{data.HotelName}</strong>.</p>
                        <p><strong>Amount Paid:</strong> ${data.Amount:N2}</p>
                        <p><strong>Payment Method:</strong> {data.PaymentMethod}</p>
                        <p><strong>Transaction ID:</strong> {data.TransactionId}</p>
                        <p><strong>Payment Date:</strong> {data.PaymentDate:MMMM dd, yyyy hh:mm tt}</p>
                        <p>Your booking is now fully confirmed. A complete itinerary has been sent to your email.</p>
                        <p>If you have any questions, feel free to reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private string BuildResendConfirmationEmail(ResendConfirmationData data)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Your Booking Details (Resent)</title>
                <style>
                    body {{ font-family: Arial, sans-serif; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
                    .header {{ background: #337ab7; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Your Booking Confirmation (Resent)</h1>
                    </div>
                    <div class='content'>
                        <p>Dear Guest,</p>
                        <p>As requested, here are your booking details again:</p>
                        <p><strong>Booking Reference:</strong> {data.BookingReference}</p>
                        <p><strong>Hotel:</strong> {data.HotelName}</p>
                        <p><strong>Check-in:</strong> {data.CheckInDate:MMMM dd, yyyy}</p>
                        <p><strong>Check-out:</strong> {data.CheckOutDate:MMMM dd, yyyy}</p>
                        <p><strong>Total Paid:</strong> ${data.TotalPrice:N2}</p>
                        <p>You can also log into your account to view, modify, or cancel your booking.</p>
                        <p>Thank you for choosing us.</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}