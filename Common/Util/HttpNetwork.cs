namespace EggLink.DanhengServer.Util;

public static class HttpNetwork
{
    public static async ValueTask<(int, string?)> SendGetRequest(string url, int timeout = 30)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(timeout);
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            return ((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            return (500, ex.Message);
        }
    }
}