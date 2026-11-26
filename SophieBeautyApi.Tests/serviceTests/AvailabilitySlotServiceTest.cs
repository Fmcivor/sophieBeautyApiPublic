using Moq;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;
using sophieBeautyApi.services;

public class AvailabilitySlotServiceTests
{
    private readonly Mock<IAvailabilitySlotRepository> _slotRepoMock;
    private readonly Mock<IBookingRepository> _bookingRepoMock;
    private readonly availablilitySlotService _sut;

    public AvailabilitySlotServiceTests()
    {
        _slotRepoMock = new Mock<IAvailabilitySlotRepository>();
        _bookingRepoMock = new Mock<IBookingRepository>();
        _sut = new availablilitySlotService(_slotRepoMock.Object, _bookingRepoMock.Object);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static availablilitySlot MakeSlot(TimeSpan start, TimeSpan end, string date = "2026-03-12") => new availablilitySlot
    {
        date = date,
        startTime = start,
        endTime = end
    };

    private static booking MakeBooking(DateTime apptDate, int duration) => new booking(
        customerName: "Sophie Test",
        appointmentDate: apptDate,
        email: "test@test.com",
        phoneNumber:"07709797855",
        treatmentNames: new List<string> { "facial" },
        cost: 50,
        duration: duration,
        payByCard: false,
        paid: false,
        bookingStatus: booking.status.Confirmed
    );

    private static readonly TimeZoneInfo UkZone = 
        TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

    private static DateTime UkToUtc(int year, int month, int day, int hour, int minute) =>
        TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(year, month, day, hour, minute, 0), UkZone);

    // ── getAll ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task getAll_ReturnsAllSlots()
    {
        _slotRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<availablilitySlot> 
            { 
                MakeSlot(new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0)) 
            });

        var result = await _sut.getAll();

        Assert.Single(result);
    }

    [Fact]
    public async Task getAll_NoSlots_ReturnsEmpty()
    {
        _slotRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<availablilitySlot>());

        var result = await _sut.getAll();

        Assert.Empty(result);
    }

    // ── create ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task create_NoExistingSlots_CreatesSlot()
    {
        var newSlot = MakeSlot(new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0));

        _slotRepoMock.Setup(r => r.GetSlotsByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<availablilitySlot>());

        _slotRepoMock.Setup(r => r.CreateAsync(newSlot))
            .ReturnsAsync(newSlot);

        var result = await _sut.create(newSlot);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task create_OverlappingSlot_ReturnsNull()
    {
        var existingSlot = MakeSlot(new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0));
        var newSlot = MakeSlot(new TimeSpan(11, 0, 0), new TimeSpan(15, 0, 0));

        _slotRepoMock.Setup(r => r.GetSlotsByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<availablilitySlot> { existingSlot });

        var result = await _sut.create(newSlot);

        Assert.Null(result);
    }

    [Fact]
    public async Task create_NonOverlappingSlot_CreatesSlot()
    {
        var existingSlot = MakeSlot(new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0));
        var newSlot = MakeSlot(new TimeSpan(13, 0, 0), new TimeSpan(17, 0, 0));

        _slotRepoMock.Setup(r => r.GetSlotsByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<availablilitySlot> { existingSlot });

        _slotRepoMock.Setup(r => r.CreateAsync(newSlot))
            .ReturnsAsync(newSlot);

        var result = await _sut.create(newSlot);

        Assert.NotNull(result);
    }

    // ── bookingWithinAvailabilitySlot ─────────────────────────────────────────
    [Fact]
    public async Task bookingWithinAvailabilitySlot_NoSlots_ReturnsFalse()
    {
        _slotRepoMock.Setup(r => r.GetSlotsByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<availablilitySlot>());

        var result = await _sut.bookingWithinAvailabilitySlot(
            UkToUtc(2026, 3, 12, 10, 0), 60);

        Assert.False(result);
    }

    [Fact]
    public async Task bookingWithinAvailabilitySlot_BookingFitsInSlot_NoExistingBookings_ReturnsTrue()
    {
        _slotRepoMock.Setup(r => r.GetSlotsByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<availablilitySlot>
            {
                MakeSlot(new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0))
            });

        _bookingRepoMock.Setup(r => r.GetBookingsByDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<booking>());

        var result = await _sut.bookingWithinAvailabilitySlot(
            UkToUtc(2026, 3, 12, 10, 0), 60);

        Assert.True(result);
    }

    [Fact]
    public async Task bookingWithinAvailabilitySlot_BookingOutsideSlot_ReturnsFalse()
    {
        _slotRepoMock.Setup(r => r.GetSlotsByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<availablilitySlot>
            {
                MakeSlot(new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0))
            });

        _bookingRepoMock.Setup(r => r.GetBookingsByDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<booking>());

        // 15:00 is outside 09:00-13:00
        var result = await _sut.bookingWithinAvailabilitySlot(
            UkToUtc(2026, 3, 12, 15, 0), 60);

        Assert.False(result);
    }

    [Fact]
    public async Task bookingWithinAvailabilitySlot_OverlapsExistingBooking_ReturnsFalse()
    {
        _slotRepoMock.Setup(r => r.GetSlotsByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<availablilitySlot>
            {
                MakeSlot(new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0))
            });

        _bookingRepoMock.Setup(r => r.GetBookingsByDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<booking>
            {
                MakeBooking(UkToUtc(2026, 3, 12, 10, 0), 60)
            });

        // New booking also at 10:00 — should overlap
        var result = await _sut.bookingWithinAvailabilitySlot(
            UkToUtc(2026, 3, 12, 10, 0), 60);

        Assert.False(result);
    }

    [Fact]
    public async Task bookingWithinAvailabilitySlot_BookingExceedsSlotEnd_ReturnsFalse()
    {
        _slotRepoMock.Setup(r => r.GetSlotsByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<availablilitySlot>
            {
                MakeSlot(new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0))
            });

        _bookingRepoMock.Setup(r => r.GetBookingsByDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<booking>());

        // 12:30 + 60 mins = 13:30 which exceeds slot end of 13:00
        var result = await _sut.bookingWithinAvailabilitySlot(
            UkToUtc(2026, 3, 12, 12, 30), 60);

        Assert.False(result);
    }

    // ── getAvailableTimes ─────────────────────────────────────────────────────
    [Fact]
    public async Task getAvailableTimes_NoExistingBookings_ReturnsAllHalfHourSlots()
    {
        _slotRepoMock.Setup(r => r.GetSlotsByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<availablilitySlot>
            {
                MakeSlot(new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0))
            });

        _bookingRepoMock.Setup(r => r.GetBookingsByDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<booking>());

        var result = await _sut.getAvailableTimes(new availableTimesRequest
        {
            date = new DateTime(2026, 3, 12),
            bookingDuration = 60
        });

        Assert.Contains(new TimeSpan(9, 0, 0), result);
        Assert.Contains(new TimeSpan(9, 30, 0), result);
        Assert.Contains(new TimeSpan(10, 0, 0), result);
    }

    [Fact]
    public async Task getAvailableTimes_ExistingBooking_ExcludesOverlappingSlots()
    {
        _slotRepoMock.Setup(r => r.GetSlotsByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<availablilitySlot>
            {
                MakeSlot(new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0))
            });

        _bookingRepoMock.Setup(r => r.GetBookingsByDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<booking>
            {
                MakeBooking(UkToUtc(2026, 3, 12, 10, 0), 60)
            });

        var result = await _sut.getAvailableTimes(new availableTimesRequest
        {
            date = new DateTime(2026, 3, 12),
            bookingDuration = 60
        });

        Assert.DoesNotContain(new TimeSpan(9, 30, 0), result);  // 09:30-10:30 overlaps
        Assert.DoesNotContain(new TimeSpan(10, 0, 0), result);  // 10:00-11:00 exact overlap
        Assert.Contains(new TimeSpan(9, 0, 0), result);         // 09:00-10:00 free
        Assert.Contains(new TimeSpan(11, 0, 0), result);        // 11:00-12:00 free
    }

    [Fact]
    public async Task getAvailableTimes_NoSlots_ReturnsEmpty()
    {
        _slotRepoMock.Setup(r => r.GetSlotsByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<availablilitySlot>());

        _bookingRepoMock.Setup(r => r.GetBookingsByDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<booking>());

        var result = await _sut.getAvailableTimes(new availableTimesRequest
        {
            date = new DateTime(2026, 3, 12),
            bookingDuration = 60
        });

        Assert.Empty(result);
    }

    // ── delete ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task delete_ValidSlot_ReturnsTrue()
    {
        var slot = MakeSlot(new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0));
        _slotRepoMock.Setup(r => r.DeleteAsync(slot))
            .ReturnsAsync(true);

        var result = await _sut.delete(slot);

        Assert.True(result);
    }

    [Fact]
    public async Task delete_SlotNotFound_ReturnsFalse()
    {
        var slot = MakeSlot(new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0));
        _slotRepoMock.Setup(r => r.DeleteAsync(slot))
            .ReturnsAsync(false);

        var result = await _sut.delete(slot);

        Assert.False(result);
    }

    // ── deleteAll ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task deleteAll_Success_ReturnsTrue()
    {
        _slotRepoMock.Setup(r => r.DeleteAllAsync())
            .ReturnsAsync(true);

        var result = await _sut.deleteAll();

        Assert.True(result);
    }

    [Fact]
    public async Task deleteAll_Failure_ReturnsFalse()
    {
        _slotRepoMock.Setup(r => r.DeleteAllAsync())
            .ReturnsAsync(false);

        var result = await _sut.deleteAll();

        Assert.False(result);
    }

    [Fact]
    public async Task deleteAll_CallsRepositoryOnce()
    {
        _slotRepoMock.Setup(r => r.DeleteAllAsync())
            .ReturnsAsync(true);

        await _sut.deleteAll();

        _slotRepoMock.Verify(r => r.DeleteAllAsync(), Times.Once);
    }
}