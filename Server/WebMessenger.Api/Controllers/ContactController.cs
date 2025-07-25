using Microsoft.AspNetCore.Mvc;
using WebMessenger.Api.Models;
using WebMessenger.Api.Services.Interfaces;

namespace WebMessenger.Api.Controllers
{
    [ApiController]
    [Route("api/contacts")]
    public class ContactController(ILogger<ContactController> logger,
                              IUserService userService,
                              IContactsService contactsService) : ControllerBase
    {
        private readonly ILogger<ContactController> _logger = logger;
        private readonly IUserService _userService = userService;
        private readonly IContactsService _contactsService = contactsService;



        [HttpGet]
        public async Task<IActionResult> Index([FromHeader(Name = "Authorization")] string authHeader, string query = "")
        {
            try
            {
                var currentUserId = await _userService.GetUserIdFromAuthHeader(authHeader);

                if (!currentUserId.HasValue)
                    return BadRequest("Authentication problems occurred");

                var contacts = await _contactsService.GetContactsAsync(currentUserId.Value, query);

                var result = contacts.ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts");
                return StatusCode(500, "An error occurred while getting contacts");
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddContact([FromHeader(Name = "Authorization")] string authHeader, [FromBody] AddContactRequest request)
        {
            try
            {
                var currentUserId = await _userService.GetUserIdFromAuthHeader(authHeader);
                var contactUserId = request.ContactUserId;

                if (!currentUserId.HasValue)
                    return BadRequest("Authentication problems occurred");

                if (currentUserId == contactUserId)
                    return BadRequest("Cannot add yourself as a contact");

                if (await _contactsService.IsContactAsync(currentUserId.Value, contactUserId))
                    return BadRequest("User is already in your contacts");

                var response = await _contactsService.AddContactAsync(currentUserId.Value, request);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding contact");
                return StatusCode(500, "An error occurred while adding contact");
            }
        }
    }
}
