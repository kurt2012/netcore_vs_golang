using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .Build();

        var host = new WebHostBuilder()
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseConfiguration(config)
            .UseUrls("http://*:5000")
            .UseStartup<Startup>();

        return host;
    }
}



class Response
{
    public string Id { get; set; }
    public string Name { get; set; }
    public long Time { get; set; }
}

class ResponseArray
{

    private static char[] _id = nameof(Response.Id).ToCharArray();
    private static char[] _name = nameof(Response.Name).ToCharArray();
    private static char[] _time = nameof(Response.Time).ToCharArray();

    public List<Response> Data { get; set; }

    public string MarshalJson()
    {
        var json = new StringBuilder().Append("[");
        foreach (var item in Data)
        {
            json.Append($"{{\"Id\":\"{item.Id}\",\"Name\":\"{item.Name}\",\"Time\":{item.Time}}},");
        }
        json.Append("]");
        return json.ToString();
    }

    public void UnmarshalJson(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        Data = JsonConvert.DeserializeObject<List<Response>>(str);

    }

    public void UnmarshalJson2(ReadOnlySpan<char> t)
    {
        Data = new List<Response>();
        Data.Capacity = 16;

        int startKey = -1;
        int endKey = -1;
        int startValue = -1;
        int endValue = -1;
        Response el = null;

        for (int i = 0; i < t.Length; i++)
        {

            if (startValue != -1)
            {
                bool end = t[i] == '}';   
                if (t[i] == ',' || end)
                {
                    endValue = end ? i : i - 1;

                    var key = t.Slice(startKey, endKey - startKey);
                    var val = t.Slice(startValue, endValue - startValue);
                
                    if (key.SequenceEqual(_id))
                    {
                        el.Id = val.ToString();
                    }
                    if (key.SequenceEqual(_name))
                    {
                        el.Name = val.ToString();
                    }
                    if (key.SequenceEqual(_time))
                    {
                        int.TryParse(val, out int time);
                        el.Time = time;
                    }


                    if (end)
                    {
                        Data.Add(el);
                    }
                    startKey = endKey = startValue = endValue = -1;
                }
                else
                {
                    continue;
                }
            }

            
            //Begin data
            if (t[i] == '{')
            {
                el = new Response();
                continue;
            }

            if (startKey == -1 || endKey == -1)
            {
                if (t[i] == '"')
                {
                    if (startKey == -1)
                    {
                        startKey = i + 1;
                        continue;
                    }
                    else
                    {
                        endKey = i;
                        i++;
                    }
                }
                else
                {
                    continue;
                }
            }
            if (t[i] == ':')
            {
                if (t[i + 1] == '"')
                {
                    startValue = i + 2;
                    i = startValue;
                }
                else
                {
                    startValue = ++i;
                }
                continue;
            }

        }

    }
}

class Startup
{
    private static readonly HttpMessageHandler _httpHandler = new HttpClientHandler
    {
        MaxConnectionsPerServer = 4000
    };

    private static readonly HttpClient _http = new HttpClient(_httpHandler)
    {
        BaseAddress = new Uri($"http://localhost:5500")
    };

    private static void HandleTest(IApplicationBuilder app)
    {
        app.Run(async ctx =>
        {
            using (var rsp = await _http.GetAsync("/data"))
            {
                var str = await rsp.Content.ReadAsStringAsync();

                // deserialize
                var obj = JsonConvert.DeserializeObject<List<Response>>(str);

                // serialize
                var json = JsonConvert.SerializeObject(obj);

                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(json);
            }
        });
    }

    private static void HandleTestNoReflection(IApplicationBuilder app)
    {
        app.Run(async ctx =>
        {
            using (var rsp = await _http.GetAsync("/data"))
            {
                var str = await rsp.Content.ReadAsStringAsync();

                var obj = new ResponseArray();
                obj.UnmarshalJson(str);
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(obj.MarshalJson());
            }
        });
    }

    private static void HandleTestNoReflection2(IApplicationBuilder app)
    {
        app.Run(async ctx =>
        {
            using (var rsp = await _http.GetAsync("/data"))
            {

                var str = await rsp.Content.ReadAsByteArrayAsync();
                var obj = new ResponseArray();
                obj.UnmarshalJson2(Encoding.UTF8.GetChars(str));

                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(obj.MarshalJson());

            }
        });
    }

        private static void HandleTestNoWork(IApplicationBuilder app)
    {
        app.Run(async ctx =>
        {
            using (var rsp = await _http.GetAsync("/data"))
            {

                var str = await rsp.Content.ReadAsStringAsync();               
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(str);

            }
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.Map("/testReflection", HandleTest);
        //app.Map("/testEasy", HandleTestNoReflection);
        app.Map("/testNoReflection", HandleTestNoReflection2);
        app.Map("/testNoProcess", HandleTestNoWork);
        app.Run(async ctx =>
        {
            await ctx.Response.WriteAsync($"Hello, {ctx.Request.Path}");
        });
    }
}
