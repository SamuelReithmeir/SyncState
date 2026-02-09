using Microsoft.EntityFrameworkCore;
using SyncState.Configuration;
using SyncState.EntityFrameworkCore.Configuration;
using SyncState.ReloadInterval;
using SyncState.Sample.Domain;
using SyncState.Sample.DTOs;
using SyncState.Sample.Events;
using SyncState.Sample.Hubs;
using SyncState.Sample.Infrastructure;
using SyncState.Sample.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure EF Core with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=sample.db";
builder.Services.AddDbContext<SampleDbContext>(options =>
    options.UseSqlite(connectionString));

// Register ActiveUserStore and SignalR
builder.Services.AddSingleton<IActiveUserStore, ActiveUserStore>();
builder.Services.AddSignalR();

builder.Services.AddSyncState(config =>
{
    config.AddState<ApplicationStateDto>(state =>
    {
        state.Property(x => x.CurrentActiveUserCount)
            .GatherFrom<IActiveUserStore>(x => x.GetActiveUsers().Count)
            .ReloadEvery(TimeSpan.FromSeconds(5))
            .Emit<ActiveUserCountChangedEvent>(x=>new ActiveUserCountChangedEvent(x));
        state.Collection(x => x.Orders)
            .WithKey(x => x.Id)
            .GatherFromAsync<SampleDbContext>(async (db, ct) => await db.Orders.Select(x => x.ToDto()).ToListAsync(ct))
            .WithEfCoreProvider()
            .FromEntity<Order>(x => x.Id)
            .WithAdditionalEntity<OrderItem>(x => (int?)x.OrderId)
            .WithMapping(x => x.ToDto());
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors();
app.UseTransactionMiddleware();

// Serve static files
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<ApplicationStateHub>("/hubs/applicationstate");

app.Run();