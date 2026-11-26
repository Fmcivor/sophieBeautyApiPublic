using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace sophieBeautyApi.Models
{
    public class availableTimesRequest
    {
       

        [Required]
        public DateTime date { get; set; }

        [Required]
        public int bookingDuration { get; set; }


    }
}