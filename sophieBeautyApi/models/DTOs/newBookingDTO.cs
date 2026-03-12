using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace sophieBeautyApi.Models
{
    public class newBookingDTO
    {


        [Required]
        [RegularExpression(@"^[A-Za-z]+(?: [A-Za-z]+)*$")]
        public string customerName { get; set; }

        [Required]
        public DateTime appointmentDate {get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> treatmentIds { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}$",ErrorMessage ="Email not of valid format.")]
        public string email { get; set; }

        [Required]
        public bool payByCard { get; set; }

      

        


     

    }
}
