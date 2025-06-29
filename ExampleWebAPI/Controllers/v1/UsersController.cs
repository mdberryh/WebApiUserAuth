using Asp.Versioning;
using CellPhoneContactsAPI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CellPhoneContactsAPI.Controllers.v1;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
public class UsersController : ControllerBase
{

    private readonly IConfiguration _config;

    public UsersController(IConfiguration configuration)
    {
        _config = configuration;
    }

    // GET: api/<UsersController>
    [HttpGet]
   // [Authorize]
    public IEnumerable<string> Get()
    {
        return new string[] { "value1", "value2" };
    }

    // GET api/<UsersController>/5
    [HttpGet("{id}")]
    //[Authorize(Policy = PolicyConstants.MustHaveEmployeeId)]
    //[Authorize(Policy = "MustBeOwner")] // both must apply
    public string Get(int id)
    {
        return _config.GetConnectionString("Default");
    }

    // POST api/<UsersController>
    [HttpPost]
    public void Post([FromBody] string value)
    {
    }

    // PUT api/<UsersController>/5
    // updates a whole record
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }
    //PATCH updates part of a record.

    // DELETE api/<UsersController>/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
}
