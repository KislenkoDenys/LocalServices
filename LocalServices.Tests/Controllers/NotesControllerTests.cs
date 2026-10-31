using LocalServices.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LocalServices.Tests.Controllers;

public class NotesControllerTests
{
    [Fact]
    public void Index_ReturnsViewResult_WithTitle()
    {
        var controller = new NotesController();

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Нотатки", controller.ViewData["Title"]);
    }
}

