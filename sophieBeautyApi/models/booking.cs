using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace sophieBeautyApi.Models
{
    public class booking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }

        [Required]
        [RegularExpression(@"^[A-Za-z]+(?: [A-Za-z]+)*$")]
        public string customerName { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}$",ErrorMessage ="Email not of valid format.")]
        public string email { get; set; }

        [Required]
        public DateTime appointmentDate { get; set; }


        [Required]
        public List<string> treatmentNames { get; set; }

        [Required]
        public int duration {get; set;}

        [Required]
        public int cost { get; set; }

        [Required]
        public bool payByCard { get; set; }

        [Required]
        public bool paid { get; set; }

        [Required]
        public status bookingStatus { get; set; }

        public string? stripeId { get; set; }

        [Required]
        public bool reminderSent { get; set; }


        public enum status
        {
            Confirmed,
            Completed
        }


        public booking(
            string customerName,
            DateTime appointmentDate,
            string email,
            List<string> treatmentNames,
            int cost,
            int duration,
            bool payByCard,
            bool paid,
            status bookingStatus)
        {
            Id = null;
            this.customerName = customerName;
            this.appointmentDate = appointmentDate;
            this.email = email;
            this.treatmentNames = treatmentNames;
            this.cost = cost;
            this.duration = duration;
            this.payByCard = payByCard;
            this.paid = paid;
            this.bookingStatus = bookingStatus;
            this.reminderSent = false;
        }

    }
}
