# Not run

- API import against SQL Server was not run. SQL Server is present, but the API's configured `DefaultConnection` targets `StudentInternshipMgmtDb` and there is no separate test connection configured for `StudentInternshipMgmtDb_Test`. To avoid touching the real database, measurements use `StudentService.ImportStudentsAsync` with EF Core InMemory and the specified CSV samples instead.
