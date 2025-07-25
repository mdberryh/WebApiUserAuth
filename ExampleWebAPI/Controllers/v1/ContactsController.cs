﻿using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CellPhoneContactsAPI.Controllers.v1;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
//[ApiVersion("2.0")]
public class ContactsController : ControllerBase
{
    // GET: api/<ContactsController>
    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new string[] { "value1", "value2" };
    }

    // GET api/<ContactsController>/5
    [HttpGet("{id}")]
    public string Get(int id)
    {
        return "value";
    }


    //Creates a record
    // POST api/<ContactsController>
    [HttpPost]
    public void Post([FromBody] string firname)
    {
    }
    // PUT api/<ContactsController>/5
    [HttpPatch("email/{id}")]
    public void PatchEmail(int id, [FromBody] List<string> email)
    {
    }
    // PUT api/<ContactsController>/5
    [HttpPatch("phone/{id}")]
    public void PatchPhone(int id, [FromBody] List<string> phone)
    {
    }

    // PUT api/<ContactsController>/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/<ContactsController>/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
}
