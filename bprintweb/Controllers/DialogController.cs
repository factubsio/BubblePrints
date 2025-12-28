using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace bprintweb.Controllers;

[Route("dlg")]
[ApiController]
public class DialogController : ControllerBase
{

    [HttpGet("graph/{guidStr}", Name = "DialogTree")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Client)]
    public IActionResult GetGraph(string guidStr)
    {
        var gameData = (HttpContext.Items["game"] as GameData)!;
        if (!Guid.TryParse(guidStr, out var guid))
        {
            return NotFound();
        }

        var normalized = guid.ToString("N");

        var path = Path.Combine(gameData.BaseDialogPath, $"{normalized}.json.gz");

        if (Path.Exists(path))
        {
            HttpContext.Response.Headers.ContentEncoding = "gzip";
            return PhysicalFile(path, "application/json");
        }

        return NotFound();
    }
}
