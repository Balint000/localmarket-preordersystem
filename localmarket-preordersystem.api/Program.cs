using localmarket_preordersystem.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PreorderDbContext>(
    options => options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();


app.Run();