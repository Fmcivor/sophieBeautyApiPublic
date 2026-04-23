using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;
using sophieBeautyApi.ServiceInterfaces;

namespace sophieBeautyApi.services
{
    public class adminService
    {

        private readonly IAdminRepository _adminRepository;

        private readonly IBookingService _bookingService;
        private readonly IEmailService _emailService;

        public adminService(IAdminRepository adminRepository, IEmailService emailService, IBookingService bookingService)
        {
            this._adminRepository = adminRepository;
            this._bookingService = bookingService;
            this._emailService = emailService;
        }

        public async Task<admin> register(adminDTO admin)
        {

            byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
            string hashed = hashPassword(admin.password,salt);

            var newAdmin = new admin();
            newAdmin.username = admin.username;
            newAdmin.password = hashed;
            newAdmin.salt = salt;

            await _adminRepository.RegisterAsync(newAdmin);

            return newAdmin;
        }

        public async Task<admin?> validateLogin(adminDTO loginDto)
        {
            var account = await _adminRepository.findAdminByUsername(loginDto.username);

            if (account == null)
            {
                return null;
            }

            string hashed = hashPassword(loginDto.password, account.salt);

            if (hashed != account.password)
            {
                return null;
            }

            return account;
        }


        private string hashPassword(string password, byte[] salt)
        {
            
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8
            ));

            return hashed;
        }


        public async Task remindBookings()
        {
            var bookings = await _bookingService.getNextDayBookings(DateTime.UtcNow);
            foreach (var booking in bookings)
            {
                if (booking.reminderSent == false)
                {

                await _emailService.sendReminder(booking);
                await _bookingService.markReminderSent(booking);
                }
            }
        }

    }
}
