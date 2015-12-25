using System.Collections.Generic;
using Homeworld.Tracker.Web.Dtos;

namespace Homeworld.Tracker.Web.Domain
{
    public interface IPeopleService
    {
        IEnumerable<PersonDto> GetInLocation(int id);
    }
}
