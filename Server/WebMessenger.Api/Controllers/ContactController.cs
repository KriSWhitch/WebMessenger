using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebMessenger.Api.Infrastructure.Interfaces;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Contracts.Models;

namespace WebMessenger.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/contacts")]
    public class ContactController(
        ILogger<ContactController> logger,
        IContactsService contactsService,
        ICurrentUser currentUser) : ControllerBase
    {
        private readonly ILogger<ContactController> _logger = logger;
        private readonly IContactsService _contactsService = contactsService;
        private readonly ICurrentUser _currentUser = currentUser;

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string query = "")
        {
            try
            {
                var contacts = await _contactsService.GetContactsAsync(_currentUser.Id, query);
                return Ok(contacts.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contacts for user {UserId}", _currentUser.Id);
                return StatusCode(500, new { message = "An error occurred while fetching contacts" });
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddContact([FromBody] AddContactRequest request)
        {
            if (_currentUser.Id == request.ContactUserId)
                return BadRequest("Cannot add yourself as a contact");

            if (await _contactsService.IsContactAsync(_currentUser.Id, request.ContactUserId))
                return BadRequest("User is already in your contacts");

            try
            {
                var response = await _contactsService.AddContactAsync(_currentUser.Id, request);
                _logger.LogDebug("Contact {ContactId} added by user {UserId}", request.ContactUserId, _currentUser.Id);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation adding contact {ContactId} for user {UserId}", request.ContactUserId, _currentUser.Id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding contact {ContactId} for user {UserId}", request.ContactUserId, _currentUser.Id);
                return StatusCode(500, new { message = "An error occurred while adding contact" });
            }
        }
    }
}