using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace sophieBeautyApi.Models
{
    public class adminDTO
    {
       

        [Required]
        public string username { get; set; }

        [Required]
        public string password { get; set; }


    }
}