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

namespace sophieBeautyApi.services
{
    public class adminService
    {

        private readonly IAdminRepository _adminRepository;

        private readonly bookingService _bookingService;
        private readonly emailService _emailService;
        private readonly jwtTokenHandler _jwtTokenHandler;

        public adminService(IAdminRepository adminRepository, emailService emailService, bookingService bookingService, jwtTokenHandler jwtTokenHandler)
        {
            this._adminRepository = adminRepository;
            this._bookingService = bookingService;
            this._emailService = emailService;
            this._jwtTokenHandler = jwtTokenHandler;
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

        public async Task<string?> login(adminDTO loginDto)
        {
            var account = await _adminRepository.findAdminByUsername(loginDto.username);

            if (account == null)
            {
                return null;
            }

            string hashed = hashPassword(loginDto.password, account.salt);

            if (!CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(hashed), Convert.FromBase64String(account.password)))
            {
                return null;
            }

            var token = _jwtTokenHandler.generateToken(account);

            return token;
        }


        private string hashPassword(string password, byte[] salt)
        {
            
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 600000,
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