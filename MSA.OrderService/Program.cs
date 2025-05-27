using MSA.Common.Contracts.Settings;
using MSA.Common.PostgresMassTransit.MassTransit;
using MSA.Common.PostgresMassTransit.PostgresDB;
using MSA.OrderService.Domain;
using MSA.OrderService.Infrastructure.Data;
using MSA.OrderService.Services;
using MSA.OrderService.StateMachine;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using MSA.OrderService.Infrastructure.Saga;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Load Postgres settings
var serviceSetting = builder.Configuration
    .GetSection(nameof(PostgresDBSetting))
    .Get<PostgresDBSetting>();

// Register services
builder.Services
    .AddPostgres<MainDbContext>()
    .AddPostgresRepositories<MainDbContext, Order>()
    .AddPostgresRepositories<MainDbContext, Product>()
    .AddPostgresUnitofWork<MainDbContext>()
    //.AddMassTransitWithRabbitMQ()
    .AddMassTransitWithPostgresOutbox<MainDbContext>(cfg =>
    {
        cfg.AddSagaStateMachine<OrderStateMachine, OrderState>()
           .EntityFrameworkRepository(r =>
           {
               r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
               r.LockStatementProvider = new PostgresLockStatementProvider();

               r.AddDbContext<DbContext, OrderStateDbContext>((provider, builder) =>
               {
                   builder.UseNpgsql(serviceSetting.ConnectionString, n =>
                   {
                       n.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                       n.MigrationsHistoryTable($"__{nameof(OrderStateDbContext)}");
                   });
               });
           });
    });

builder.Services.AddHttpClient<IProductService, ProductService>(cfg => 
{
        cfg.BaseAddress = new Uri("http://localhost:5002");
});

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply migrations for MainDbContext
using (var scope = app.Services.CreateScope())
{
    var mainDb = scope.ServiceProvider.GetRequiredService<MainDbContext>();
    mainDb.Database.Migrate();

    var sagaDb = scope.ServiceProvider.GetRequiredService<OrderStateDbContext>();
    sagaDb.Database.Migrate();
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();
