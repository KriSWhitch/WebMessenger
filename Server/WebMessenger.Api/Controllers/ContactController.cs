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
            var contacts = await _contactsService.GetContactsAsync(_currentUser.Id, query);
            return Ok(contacts.ToList());
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddContact([FromBody] AddContactRequest request)
        {
            if (_currentUser.Id == request.ContactUserId)
                return BadRequest("Cannot add yourself as a contact");

            if (await _contactsService.IsContactAsync(_currentUser.Id, request.ContactUserId))
                return BadRequest("User is already in your contacts");

            var response = await _contactsService.AddContactAsync(_currentUser.Id, request);
            return Ok(response);
        }
    }
}