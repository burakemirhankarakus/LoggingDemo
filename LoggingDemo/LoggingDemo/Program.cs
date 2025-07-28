using Amazon.CloudWatchLogs;
using Amazon.Runtime;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.AwsCloudWatch;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;

var builder = WebApplication.CreateBuilder(args);

// Connection string'i al (MSSQL için)
var connectionString = builder.Configuration.GetConnectionString("LogDb");

//  MSSQL log kolonlarını tanımla
var columnOptions = new ColumnOptions
{
    Store = new Collection<StandardColumn>
    {
        StandardColumn.Message,
        StandardColumn.MessageTemplate,
        StandardColumn.Level,
        StandardColumn.TimeStamp,
        StandardColumn.Exception,
        StandardColumn.Properties,
        StandardColumn.LogEvent
    }
};

// AWS CloudWatch client oluştur
var awsSection = builder.Configuration.GetSection("AWS");
var awsCredentials = new BasicAWSCredentials(
    awsSection["AccessKey"],
    awsSection["SecretKey"]
);

var cloudWatchClient = new AmazonCloudWatchLogsClient(
    awsCredentials,
    Amazon.RegionEndpoint.EUCentral1 // İstanbul ise: MECentral1
);

var cloudWatchOptions = new CloudWatchSinkOptions
{
    LogGroupName = "LoggingDemoGroup",
    LogStreamNameProvider = new DefaultLogStreamProvider(),
    TextFormatter = new RenderedCompactJsonFormatter(),
    CreateLogGroup = true,
    MinimumLogEventLevel = LogEventLevel.Information
};

// Serilog Logger yapılandırması
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.MSSqlServer(
        connectionString: connectionString,
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "Logs",
            AutoCreateSqlTable = false
        },
        columnOptions: columnOptions
    )
    .WriteTo.AmazonCloudWatch(cloudWatchOptions, cloudWatchClient)
    .CreateLogger();

//  Serilog'u host'a entegre et
builder.Host.UseSerilog();

//  API ayarları
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

//  Test endpoint
app.MapGet("/", () =>
{
    Log.Information("Ana endpoint çağrıldı.");
    return Results.Ok("Merhaba, log sistemi çalışıyor!");
});

app.Run();
