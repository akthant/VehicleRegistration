using System;
using System.Collections.Generic;
using System.Text;
using VehicleRegistration.Core.Entities;

namespace VehicleRegistration.Core.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public int ManufacturerId { get; set; }
        public int YearOfManufacture { get; set; }
        public decimal Weight { get; set; }  // 2 decimal places, in kg
        // Navigation properties
        public Owner Owner { get; set; } = null!;
        public Manufacturer Manufacturer { get; set; } = null!;

        //Category is NOT stored - computed at runtime from Weight
    }
}
