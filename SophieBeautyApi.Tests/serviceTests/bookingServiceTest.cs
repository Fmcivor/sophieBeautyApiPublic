using Moq;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;
using sophieBeautyApi.ServiceInterfaces;
using sophieBeautyApi.services;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepoMock;
    private readonly Mock<IAvailabilitySlotService> _availabilityServiceMock;
    private readonly Mock<ITreatmentService> _treatmentServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly bookingService _sut;

    public BookingServiceTests()
    {
        _bookingRepoMock = new Mock<IBookingRepository>();
        _availabilityServiceMock = new Mock<IAvailabilitySlotService>();
        _treatmentServiceMock = new Mock<ITreatmentService>();
        _emailServiceMock = new Mock<IEmailService>();

        _sut = new bookingService(
            _bookingRepoMock.Object,
            _availabilityServiceMock.Object,
            _treatmentServiceMock.Object,
            _emailServiceMock.Object
        );
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static booking MakeBooking(string id = "abc123", DateTime? date = null) => new booking(
        customerName: "Sophie Test",
        appointmentDate: date ?? DateTime.UtcNow,
        email: "test@test.com",
        phoneNumber: "07709797855",
        treatmentNames: new List<string> { "facial" },
        cost: 50,
        duration: 60,
        payByCard: false,
        paid: false,
        bookingStatus: booking.status.Confirmed
    )
    { Id = id };

    private static treatment MakeTreatment() => new treatment
    {
        Id = "t1",
        name = "facial",
        price = 50,
        duration = 60,
        type = "face",
        description = "A relaxing facial treatment for all skin types available"
    };

    private static newBookingDTO MakeBookingDTO() => new newBookingDTO
    {
        customerName = "Sophie Test",
        appointmentDate = DateTime.UtcNow,
        email = "test@test.com",
        phoneNumber = "07709797855",
        treatmentIds = new List<string> { "t1" },
        payByCard = false
    };

    // ── getAll ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task getAll_ReturnsBookingsWithUkTime()
    {
        var utcDate = new DateTime(2026, 3, 12, 10, 0, 0, DateTimeKind.Utc);

        _bookingRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<booking> { MakeBooking(date: utcDate) });

        var result = await _sut.getAll();

        var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        var expected = TimeZoneInfo.ConvertTimeFromUtc(utcDate, ukZone);

        Assert.Single(result);
        Assert.Equal(expected, result.First().appointmentDate);
    }

    [Fact]
    public async Task getAll_NoBookings_ReturnsEmpty()
    {
        _bookingRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<booking>());

        var result = await _sut.getAll();

        Assert.Empty(result);
    }

    // ── create ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task create_ValidBooking_ReturnsSuccess()
    {
        var dto = MakeBookingDTO();
        var created = MakeBooking();

        _treatmentServiceMock.Setup(s => s.getListByIds(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<treatment> { MakeTreatment() });

        _availabilityServiceMock.Setup(s => s.bookingWithinAvailabilitySlot(It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        _bookingRepoMock.Setup(r => r.CreateAsync(It.IsAny<booking>()))
            .ReturnsAsync(created);

        _emailServiceMock.Setup(e => e.Send(It.IsAny<booking>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.create(dto);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Booking);
    }

    [Fact]
    public async Task create_SlotNotAvailable_ReturnsTaken()
    {
        _treatmentServiceMock.Setup(s => s.getListByIds(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<treatment> { MakeTreatment() });

        _availabilityServiceMock.Setup(s => s.bookingWithinAvailabilitySlot(It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var result = await _sut.create(MakeBookingDTO());

        Assert.False(result.IsSuccess);
        Assert.Equal("TAKEN", result.Error);
    }

    [Fact]
    public async Task create_RepositoryThrowsException_ReturnsServerError()
{
    _treatmentServiceMock.Setup(s => s.getListByIds(It.IsAny<List<string>>()))
        .ReturnsAsync(new List<treatment> { MakeTreatment() });

    _availabilityServiceMock.Setup(s => s.bookingWithinAvailabilitySlot(It.IsAny<DateTime>(), It.IsAny<int>()))
        .ReturnsAsync(true);

    _bookingRepoMock.Setup(r => r.CreateAsync(It.IsAny<booking>()))
        .ThrowsAsync(new Exception("DB failure"));

    var result = await _sut.create(MakeBookingDTO());

    Assert.False(result.IsSuccess);
    Assert.Equal("SERVER_ERROR", result.Error);
}

    [Fact]
    public async Task create_ValidBooking_SendsEmail()
    {
        _treatmentServiceMock.Setup(s => s.getListByIds(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<treatment> { MakeTreatment() });

        _availabilityServiceMock.Setup(s => s.bookingWithinAvailabilitySlot(It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        _bookingRepoMock.Setup(r => r.CreateAsync(It.IsAny<booking>()))
            .ReturnsAsync(MakeBooking());

        _emailServiceMock.Setup(e => e.Send(It.IsAny<booking>()))
            .Returns(Task.CompletedTask);

        await _sut.create(MakeBookingDTO());

        _emailServiceMock.Verify(e => e.Send(It.IsAny<booking>()), Times.Once);
    }

    [Fact]
    public async Task create_SlotNotAvailable_NeverCallsRepository()
    {
        _treatmentServiceMock.Setup(s => s.getListByIds(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<treatment> { MakeTreatment() });

        _availabilityServiceMock.Setup(s => s.bookingWithinAvailabilitySlot(It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        await _sut.create(MakeBookingDTO());

        _bookingRepoMock.Verify(r => r.CreateAsync(It.IsAny<booking>()), Times.Never);
    }

    // ── getById ───────────────────────────────────────────────────────────────
    [Fact]
    public async Task getById_ValidId_ReturnsBooking()
    {
        _bookingRepoMock.Setup(r => r.GetByIdAsync("abc123"))
            .ReturnsAsync(MakeBooking());

        var result = await _sut.getById("abc123");

        Assert.NotNull(result);
        Assert.Equal("abc123", result!.Id);
    }

    [Fact]
    public async Task getById_InvalidId_ReturnsNull()
    {
        _bookingRepoMock.Setup(r => r.GetByIdAsync("notreal"))
            .ReturnsAsync((booking?)null);

        var result = await _sut.getById("notreal");

        Assert.Null(result);
    }

    // ── update ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task update_ValidBooking_ReturnsTrue()
    {
        var booking = MakeBooking();
        _bookingRepoMock.Setup(r => r.UpdateAsync(booking))
            .ReturnsAsync(true);

        var result = await _sut.update(booking);

        Assert.True(result);
    }

    [Fact]
    public async Task update_NullId_ReturnsFalse()
    {
        var booking = MakeBooking();
        booking.Id = null;

        var result = await _sut.update(booking);

        Assert.False(result);
        _bookingRepoMock.Verify(r => r.UpdateAsync(It.IsAny<booking>()), Times.Never);
    }

    [Fact]
    public async Task update_RepositoryFails_ReturnsFalse()
    {
        var booking = MakeBooking();
        _bookingRepoMock.Setup(r => r.UpdateAsync(booking))
            .ReturnsAsync(false);

        var result = await _sut.update(booking);

        Assert.False(result);
    }

    // ── delete ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task delete_ValidId_ReturnsSuccess()
    {
        _bookingRepoMock.Setup(r => r.GetByIdAsync("abc123"))
            .ReturnsAsync(MakeBooking());

        _bookingRepoMock.Setup(r => r.DeleteAsync("abc123"))
            .ReturnsAsync(true);

        _emailServiceMock.Setup(e => e.sendCancellation(It.IsAny<booking>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.delete("abc123");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task delete_BookingNotFound_ReturnsNotFound()
    {
        _bookingRepoMock.Setup(r => r.GetByIdAsync("notreal"))
            .ReturnsAsync((booking?)null);

        var result = await _sut.delete("notreal");

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task delete_RepositoryFails_ReturnsServerError()
    {
        _bookingRepoMock.Setup(r => r.GetByIdAsync("abc123"))
            .ReturnsAsync(MakeBooking());

        _bookingRepoMock.Setup(r => r.DeleteAsync("abc123"))
            .ReturnsAsync(false);

        var result = await _sut.delete("abc123");

        Assert.False(result.IsSuccess);
        Assert.Equal("SERVER_ERROR", result.Error);
    }

    [Fact]
    public async Task delete_ValidId_SendsCancellationEmail()
    {
        _bookingRepoMock.Setup(r => r.GetByIdAsync("abc123"))
            .ReturnsAsync(MakeBooking());

        _bookingRepoMock.Setup(r => r.DeleteAsync("abc123"))
            .ReturnsAsync(true);

        _emailServiceMock.Setup(e => e.sendCancellation(It.IsAny<booking>()))
            .Returns(Task.CompletedTask);

        await _sut.delete("abc123");

        _emailServiceMock.Verify(e => e.sendCancellation(It.IsAny<booking>()), Times.Once);
    }

    // ── getTodaysBooking ──────────────────────────────────────────────────────
    [Fact]
    public async Task getTodaysBooking_ReturnsBookingsWithUkTime()
    {
        var utcDate = new DateTime(2026, 3, 12, 10, 0, 0, DateTimeKind.Utc);

        _bookingRepoMock.Setup(r => r.GetTodaysBookingAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<booking> { MakeBooking(date: utcDate) });

        var result = await _sut.getTodaysBooking(new DateTime(2026, 3, 12));

        // Compute the expected UK time
        var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        var expected = TimeZoneInfo.ConvertTimeFromUtc(utcDate, ukZone);

        Assert.Single(result);
        Assert.Equal(expected, result.First().appointmentDate);
    }

    [Fact]
    public async Task getTodaysBooking_NoBookings_ReturnsEmpty()
    {
        _bookingRepoMock.Setup(r => r.GetTodaysBookingAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<booking>());

        var result = await _sut.getTodaysBooking(new DateTime(2026, 3, 12));

        Assert.Empty(result);
    }

    // ── getWeeklyRevenue ──────────────────────────────────────────────────────
    [Fact]
    public async Task getWeeklyRevenue_ReturnsCorrectSum()
    {
        var bookings = new List<booking>
        {
            MakeBooking("1"),  // cost 50
            MakeBooking("2")   // cost 50
        };

        _bookingRepoMock.Setup(r => r.getBookingsByDateRange(
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), booking.status.Confirmed))
            .ReturnsAsync(bookings);

        var result = await _sut.getWeeklyRevenue(new DateTime(2026, 3, 12));

        Assert.Equal(100, result);
    }

    [Fact]
    public async Task getWeeklyRevenue_NoBookings_ReturnsZero()
    {
        _bookingRepoMock.Setup(r => r.getBookingsByDateRange(
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), booking.status.Confirmed))
            .ReturnsAsync(new List<booking>());

        var result = await _sut.getWeeklyRevenue(new DateTime(2026, 3, 12));

        Assert.Equal(0, result);
    }

    // ── getMonthlyRevenue ─────────────────────────────────────────────────────
    [Fact]
    public async Task getMonthlyRevenue_ReturnsCorrectSum()
    {
        var bookings = new List<booking>
        {
            MakeBooking("1"),  // cost 50
            MakeBooking("2"),  // cost 50
            MakeBooking("3")   // cost 50
        };

        _bookingRepoMock.Setup(r => r.getBookingsByDateRange(
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), booking.status.Confirmed))
            .ReturnsAsync(bookings);

        var result = await _sut.getMonthlyRevenue(new DateTime(2026, 3, 12));

        Assert.Equal(150, result);
    }

    [Fact]
    public async Task getMonthlyRevenue_NoBookings_ReturnsZero()
    {
        _bookingRepoMock.Setup(r => r.getBookingsByDateRange(
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), booking.status.Confirmed))
            .ReturnsAsync(new List<booking>());

        var result = await _sut.getMonthlyRevenue(new DateTime(2026, 3, 12));

        Assert.Equal(0, result);
    }

    // ── markReminderSent ──────────────────────────────────────────────────────
    [Fact]
    public async Task markReminderSent_ValidBooking_ReturnsTrue()
    {
        var booking = MakeBooking();
        _bookingRepoMock.Setup(r => r.MarkReminderSentAsync(booking))
            .ReturnsAsync(true);

        var result = await _sut.markReminderSent(booking);

        Assert.True(result);
    }

    [Fact]
    public async Task markReminderSent_RepositoryFails_ReturnsFalse()
    {
        var booking = MakeBooking();
        _bookingRepoMock.Setup(r => r.MarkReminderSentAsync(booking))
            .ReturnsAsync(false);

        var result = await _sut.markReminderSent(booking);

        Assert.False(result);
    }

    // ── getUpcomingBookings ───────────────────────────────────────────────────
    [Fact]
    public async Task getUpcomingBookings_ReturnsBookingsWithUkTime()
    {
        var utcDate = new DateTime(2026, 3, 12, 10, 0, 0, DateTimeKind.Utc);

        _bookingRepoMock.Setup(r => r.GetUpcomingBookingsAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<booking> { MakeBooking(date: utcDate) });

        var result = await _sut.getUpcomingBookings(new DateTime(2026, 3, 12));

        // Compute the expected UK time
        var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        var expected = TimeZoneInfo.ConvertTimeFromUtc(utcDate, ukZone);

        Assert.Single(result);
        Assert.Equal(expected, result.First().appointmentDate);
    }

    [Fact]
    public async Task getUpcomingBookings_NoBookings_ReturnsEmpty()
    {
        _bookingRepoMock.Setup(r => r.GetUpcomingBookingsAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<booking>());

        var result = await _sut.getUpcomingBookings(new DateTime(2026, 3, 12));

        Assert.Empty(result);
    }

    // ── getNextDayBookings ────────────────────────────────────────────────────
    [Fact]
    public async Task getNextDayBookings_ReturnsBookings()
    {
        _bookingRepoMock.Setup(r => r.GetNextDayBookingsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<booking> { MakeBooking() });

        var result = await _sut.getNextDayBookings(new DateTime(2026, 3, 12));

        Assert.Single(result);
    }

    [Fact]
    public async Task getNextDayBookings_NoBookings_ReturnsEmpty()
    {
        _bookingRepoMock.Setup(r => r.GetNextDayBookingsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<booking>());

        var result = await _sut.getNextDayBookings(new DateTime(2026, 3, 12));

        Assert.Empty(result);
    }
}