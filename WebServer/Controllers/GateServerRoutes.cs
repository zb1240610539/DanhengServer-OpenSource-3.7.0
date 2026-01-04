using EggLink.DanhengServer.Util;
using EggLink.DanhengServer.WebServer.Handler;
using Microsoft.AspNetCore.Mvc;
using EggLink.DanhengServer.WebServer.Request;
namespace EggLink.DanhengServer.WebServer.Controllers;

[ApiController]
[Route("/")]
public class GateServerRoutes
{
    [HttpGet("/query_gateway")]
    public async ValueTask<ContentResult> QueryGateway([FromQuery] GateWayRequest req)
    {
        if (!ConfigManager.Config.ServerOption.ServerConfig.RunGateway)
            return new ContentResult
            {
                StatusCode = 404
            };

        await ValueTask.CompletedTask;
        return new ContentResult
        {
            Content = new QueryGatewayHandler(req).Data,
            StatusCode = 200,
            ContentType = "plain/text; charset=utf-8"
        };
    }
}