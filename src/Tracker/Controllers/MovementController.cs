using System.Diagnostics;
using Homeworld.Tracker.Web.Domain;
using Homeworld.Tracker.Web.Dtos;
using Microsoft.AspNet.Mvc;

namespace Homeworld.Tracker.Web.Controllers
{
    [Produces("application/json")]
    [Route("api/movement")]
    public class MovementController : Controller
    {
        private readonly IMovementService _movementService;

        public MovementController(IMovementService movementService)
        {
            _movementService = movementService;
        }

        [ResponseCache(Duration = 0)]
        [HttpPost]
        public IActionResult PostMovement([FromBody] MovementDto movementDto)
        {
            if (!ModelState.IsValid)
            {
                return HttpBadRequest(ModelState);
            }

            //record movement
            var movementResult = _movementService.Save(movementDto);

            if (movementResult == null)
                return HttpNotFound();

            if (movementResult.IsError)
            {
                return HttpNotFound(movementResult.ErrorMessage);
            }

            var result = new MovementResponseDto
            {
                Id = movementResult.Person.Id,
                Name = $"{movementResult.Person.FirstName} {movementResult.Person.LastName}",
                Image = movementResult.Person.Image,
                Ingress = movementResult.Ingress
            };

            Debug.WriteLine("Id: {0} Name: {1}", result.Id, result.Name);

            return Ok(result);
        }
    }
}