using Homeworld.Tracker.Web.Domain.Model;
using Homeworld.Tracker.Web.Dtos;

namespace Homeworld.Tracker.Web.Domain
{
    public interface IMovementService
    {
        MovementResult Save(MovementDto movement);
    }
}
