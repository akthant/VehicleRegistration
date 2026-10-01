using System;
using System.Collections.Generic;
using System.Text;
using VehicleRegistration.Core.Entities;

namespace VehicleRegistration.Core.Entities
{
    public class Owner
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Navigation property
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }
}