using System;
using System.Diagnostics;
using System.Linq;
using Homeworld.Tracker.Web.Domain.Model;
using Homeworld.Tracker.Web.Dtos;
using Homeworld.Tracker.Web.Models;

namespace Homeworld.Tracker.Web.Domain
{
    public class MovementService : IMovementService
    {
        private readonly TrackerDbContext _context;

        public MovementService(TrackerDbContext context)
        {
            _context = context;
        }
        
        public MovementResult Save(MovementDto movement)
        {
            var result = new MovementResult();

            try
            {
                //check card is valid
                var card =
                    _context.Card.FirstOrDefault(
                        c => string.Equals(c.Uid, movement.CardUid, StringComparison.OrdinalIgnoreCase));

                if (card == null)
                {
                    result.IsError = true;
                    result.ErrorMessage = $"Invalid Card Id: {movement.CardUid}";
                    return result;
                }

                //record movement
                var location = (from d in _context.Device
                                join l in _context.Location on d.LocationId equals l.Id
                                where d.Id.Equals(movement.DeviceId)
                                select l).FirstOrDefault();

                if (location != null)
                {
                    //Check ingress or egress
                    var latestMovement =
                        _context.Movement
                            .OrderByDescending(m => m.SwipeTime)
                            .FirstOrDefault(m => m.CardId == movement.CardUid);

                    if (latestMovement == null)
                    {
                        //ingress
                        //first recorded movement
                        result.Ingress = true;
                    }
                    else
                    {
                        //If latest movement this location egress  
                        result.Ingress = latestMovement.LocationId != location.Id;
                    }

                    //Location 1 is off site
                    var locationId = result.Ingress ? location.Id : 1;

                    _context.Movement.Add(new Movement
                    {
                        CardId = movement.CardUid,
                        DeviceId = movement.DeviceId,
                        LocationId = locationId,
                        SwipeTime = movement.SwipeTime
                    });

                    _context.SaveChanges();
                }
                else
                {
                    result.IsError = true;
                    result.ErrorMessage = $"Device location not found deviceId: {movement.DeviceId}";
                    return result;
                }

                var person = (from c in _context.Card
                              join pc in _context.PersonCard on c.Id equals pc.CardId
                              join p in _context.Person on pc.PersonId equals p.Id
                              where c.Uid.Equals(movement.CardUid)
                              select p).FirstOrDefault();

                result.Person = person;
                result.CardUid = movement.CardUid;
            }
            catch (Exception ex)
            {
                //TODO log
                
            }

            return result;
        }
    }
}