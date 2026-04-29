using sophieBeautyApi.Models;

namespace sophieBeautyApi.ServiceInterfaces
{

    public interface IPaymentService
    {
        

        Task<String?> CreatePaymentIntent(booking b);
        

    }
}
