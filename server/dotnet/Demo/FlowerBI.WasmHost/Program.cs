using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FlowerBI.Engine.JsonModels;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace FlowerBI.WasmHost;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddTransient(sp => new HttpClient
        {
            BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
        });

        var app = builder.Build();

        JsRuntime = app.Services.GetRequiredService<IJSRuntime>();

        var task = NotifyAsync().ContinueWith(t => Console.WriteLine(t.Exception?.Message ?? "Ok"));

        await app.RunAsync();
    }

    public static async Task NotifyAsync()
    {
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(100);

            try
            {
                await JsRuntime.InvokeAsync<string>("notifyBlazorReady");
                return;
            }
            catch (Exception) { }
        }
    }

    private static IJSRuntime JsRuntime;

    private static readonly Schema Demo = new Schema(typeof(DemoSchema.BugSchema));

    public static DateTime AsUtc(DateTime dateTime) =>
        dateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            : dateTime.ToUniversalTime();

    static Program()
    {
        DemoSchema.BugSchema.Date.Id.SetConverter(AsUtc);
        DemoSchema.BugSchema.Date.FirstDayOfMonth.SetConverter(AsUtc);
        DemoSchema.BugSchema.Date.FirstDayOfQuarter.SetConverter(AsUtc);
        DemoSchema.BugSchema.Bug.AssignedDate.SetConverter(AsUtc);
        DemoSchema.BugSchema.Bug.ReportedDate.SetConverter(AsUtc);
        DemoSchema.BugSchema.Bug.ResolvedDate.SetConverter(AsUtc);
    }

    private static readonly ISqlFormatter Formatter = new SqlLiteFormatter();

    [JSInvokable]
    public static async Task<string> Query(string queryJson)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
        };

        var parsed = JsonSerializer.Deserialize<QueryJson>(queryJson, jsonOptions);
        string sql = null;

        try
        {
            var query = new Query(parsed, Demo);

            var filterParams = new EmbeddedFilterParameters();
            sql = query.ToSql(Formatter, filterParams, []);

            return await JsRuntime.InvokeAsync<string>("querySql", sql);
        }
        catch (Exception x)
        {
            x = x.GetBaseException();
            return JsonSerializer.Serialize(
                new
                {
                    message = x.Message,
                    stackTrace = x.StackTrace,
                    input = parsed,
                    sql,
                }
            );
        }
    }
}
