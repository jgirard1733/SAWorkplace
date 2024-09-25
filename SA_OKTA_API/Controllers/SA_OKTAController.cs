using SA_OKTA_API.ActionFilters;
using SA_OKTA_API.Contracts;
using InternalItems.Models;
using Microsoft.AspNetCore.Mvc;

namespace SA_OKTA_API.Controllers
{
    /// <summary>
    /// Used for calling OKTA APIs.  Currently only used for getting latest list of Okta users for iPipeline.
    /// </summary>
    [Produces("application/json")]
    [Route("api/updateusers")]
    public class SAOktaController : Controller
    {

        private readonly ISAOktaRepository _saOktaRepository;
        /// <summary>
        /// Constructor that creates an instance of the API interface.
        /// </summary>
        public SAOktaController(ISAOktaRepository saOktaRepository)
        {
            _saOktaRepository = saOktaRepository;
        }

        /// <summary>
        /// Triggers the API to make a call to OKTA for getting latest list of users and updating a DB.
        /// Does not return user list, just an OK string.  
        /// </summary>
        /// <returns>"OK"</returns>
        /// <response code="200">Returns string "OK".</response>
        /// <response code="500">OH NO! Something went wrong.</response>
        [HttpGet]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ErrorDetails), 500)]
        public IActionResult Get()
        {
            return Ok(_saOktaRepository.UpdateOktaUsers());
        }
    }
}