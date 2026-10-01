using System;
using System.Collections.Generic;
using System.Text;


namespace VehicleRegistration.Core.Entities
{
    public class VehicleCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal MinWeight { get; set; }  // Inclusive, 2 decimal places
        public decimal? MaxWeight { get; set; } // Inclusive, null = unbounded
        public string IconUrl { get; set; } = string.Empty;  // relative path to icon (max length 500)
    }
}
