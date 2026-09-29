var builder = DistributedApplication.CreateBuilder(args);

// Aqui vai o SQL Server do Aspire depois
var sql = builder.AddSqlServer("sqlserver").AddDatabase("TicTacToeDb");

builder.AddProject<Projects.TicTacToe_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(sql)
    .WaitFor(sql);

builder.Build().Run();
