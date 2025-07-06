using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CellPhoneContactsAPI.Controllers.v1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ExampleWebAPI.Tests.UnitTests;

public class UsersControllerTest
{
    private IConfiguration CreateMockConfig()
    {
        var mockConfig = new Mock<IConfiguration>();

        // Setup nested config values
        mockConfig.Setup(config => config["ConnectionStrings:Default"])
            .Returns("Host=127.0.0.1;Database=ExampleWebAPI;Username=admin;Password=secret");

        mockConfig.Setup(config => config["Authentication:SecretKey"])
            .Returns("ThisIsTheSecretKeyGUID123456789abcdefghijkl");

        mockConfig.Setup(config => config["Authentication:AppSalt"])
            .Returns("saltme24");

        mockConfig.Setup(config => config["Authentication:Issuer"])
            .Returns("http://localhost:5271");

        mockConfig.Setup(config => config["Authentication:Audience"])
            .Returns("ApiSecurityApp");

        mockConfig.Setup(config => config["Authentication:ExpireMinutes"])
            .Returns("2");

        return mockConfig.Object;
    }

    [Fact]
    public void Get_ById_ReturnsExpectedValue_WhenUserIsAuthenticated()
    {
        // Arrange
        var controller = new UsersController(configuration: CreateMockConfig()); // null is fine since we don't use it

        // Simulate an authenticated user
        var fakeUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
                new Claim(ClaimTypes.Name, "MockUser"),
                new Claim("employeeId", "123"),
                new Claim("title", "Business Owner")
            }, "TestAuthScheme"));

        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = fakeUser }
        };

        // Act
        var result = controller.Get(5);

        // Assert
        // Since your current implementation returns _config.GetConnectionString("Default"),
        // this will return null unless configuration is passed — update logic if needed
        Assert.True(result == "MockUser"); // OR whatever you expect it to return if config is null
    }
    [Fact]
    public void Get_ById_ReturnsExpectedValue_WhenUserIsNOTAuthenticated()
    {
        // Arrange
        var controller = new UsersController(configuration: CreateMockConfig()); // null is fine since we don't use it

        //// Simulate an authenticated user
        //var fakeUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        //{
        //    new Claim(ClaimTypes.Name, "MockUser"),
        //    new Claim("employeeId", "123"),
        //    new Claim("title", "Business Owner")
        //}, "TestAuthScheme"));

        //controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        //{
        //    HttpContext = new DefaultHttpContext { User = fakeUser }
        //};

        // Act
        var result = controller.Get(5);

        // Assert
        // Since your current implementation returns _config.GetConnectionString("Default"),
        // this will return null unless configuration is passed — update logic if needed
        Assert.True(result == null); // OR whatever you expect it to return if config is null
    }
}
{
}
