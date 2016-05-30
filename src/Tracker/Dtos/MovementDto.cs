using System;

namespace Homeworld.Tracker.Web.Dtos
{
    public class MovementDto
    {
        public string CardUid { get; set; }
        public int DeviceId { get; set; }
        public DateTime SwipeTime { get; set; }
    }
}