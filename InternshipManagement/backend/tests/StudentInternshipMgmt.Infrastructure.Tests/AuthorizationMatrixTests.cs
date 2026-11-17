using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using StudentInternshipMgmt.Application.Features.About;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public sealed class AuthorizationMatrixTests : IClassFixture<AuthorizationApiFactory>
{
    private static readonly HashSet<string> PublicEndpoints =
    [
        "POST /api/auth/login",
        "POST /api/auth/refresh"
    ];

    private readonly AuthorizationApiFactory _factory;

    public AuthorizationMatrixTests(AuthorizationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Every_controller_endpoint_requires_authentication_except_login_and_refresh()
    {
        await _factory.EnsureSeededAsync();
        var endpoints = GetControllerEndpoints();
        var actualPublicEndpoints = endpoints
            .Select(ToEndpointKey)
            .Where(PublicEndpoints.Contains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(PublicEndpoints.Count, actualPublicEndpoints.Count);

        foreach (var endpoint in endpoints)
        {
            var key = ToEndpointKey(endpoint);
            if (PublicEndpoints.Contains(key))
            {
                continue;
            }

            using var response = await SendEndpointAsync(endpoint);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task Role_tokens_are_denied_by_endpoints_outside_their_role_and_admin_cannot_use_me_routes()
    {
        await _factory.EnsureSeededAsync();
        var endpoints = GetControllerEndpoints();
        var studentToken = await _factory.GetTokenAsync("matrix-student-a");
        var companyToken = await _factory.GetTokenAsync("matrix-company-a");
        var adminToken = await _factory.GetTokenAsync("matrix-admin");

        await AssertDeniedForRoleAsync(endpoints, "User", studentToken);
        await AssertDeniedForRoleAsync(endpoints, "Company", companyToken);

        foreach (var endpoint in endpoints.Where(endpoint =>
                     GetPath(endpoint).StartsWith("/api/me/", StringComparison.OrdinalIgnoreCase) ||
                     GetPath(endpoint).StartsWith("/api/company-me/", StringComparison.OrdinalIgnoreCase)))
        {
            using var response = await SendEndpointAsync(endpoint, adminToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Company_cannot_read_another_company_request_or_position_and_student_cannot_report_another_students_request()
    {
        await _factory.EnsureSeededAsync();
        var companyToken = await _factory.GetTokenAsync("matrix-company-a");
        var studentToken = await _factory.GetTokenAsync("matrix-student-a");

        using var companyRequest = await SendAsync(
            HttpMethod.Get,
            $"/api/company-me/placement-requests/{_factory.CompanyBRequestId}",
            companyToken);
        Assert.Equal(HttpStatusCode.NotFound, companyRequest.StatusCode);

        using var companyPosition = await SendAsync(
            HttpMethod.Get,
            $"/api/company-me/job-positions/{_factory.CompanyBPositionId}",
            companyToken);
        Assert.Equal(HttpStatusCode.NotFound, companyPosition.StatusCode);

        using var studentReport = await SendAsync(
            HttpMethod.Post,
            $"/api/me/placement-requests/{_factory.StudentBRequestId}/report-interviewed",
            studentToken,
            JsonContent.Create(new
            {
                interviewedAt = DateTime.UtcNow,
                note = "Không thuộc hồ sơ của sinh viên hiện tại."
            }));
        Assert.Equal(HttpStatusCode.NotFound, studentReport.StatusCode);
    }

    [Fact]
    public async Task Json_responses_from_all_controller_endpoints_do_not_expose_secret_fields()
    {
        using var factory = new AuthorizationApiFactory();
        await factory.EnsureSeededAsync();
        var endpoints = GetControllerEndpoints(factory);
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Admin"] = await factory.GetTokenAsync("matrix-admin"),
            ["User"] = await factory.GetTokenAsync("matrix-student-a"),
            ["Company"] = await factory.GetTokenAsync("matrix-company-a")
        };

        Assert.NotEmpty(endpoints);
        foreach (var endpoint in endpoints)
        {
            if (PublicEndpoints.Contains(ToEndpointKey(endpoint)))
            {
                continue;
            }

            var role = SelectAllowedRole(endpoint);
            var method = new HttpMethod(GetMethod(endpoint));
            var path = method == HttpMethod.Get
                ? GetPath(endpoint)
                : GetPathWithUnmatchedRouteIds(endpoint);
            if (path.Equals("/api/students/export", StringComparison.OrdinalIgnoreCase))
            {
                path += "?format=csv";
            }

            var content = method == HttpMethod.Get || method == HttpMethod.Delete
                ? null
                : JsonContent.Create(new { });
            using var response = await SendWithClientAsync(factory.Client, method, path, tokens[role], content);
            if (!response.Content.Headers.ContentType?.MediaType?.Contains(
                    "json",
                    StringComparison.OrdinalIgnoreCase) ?? true)
            {
                continue;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            AssertNoSecretProperties(document.RootElement, ToEndpointKey(endpoint));
        }
    }

    [Fact]
    public async Task Placement_request_keeps_script_markup_as_plain_json_text()
    {
        await _factory.EnsureSeededAsync();
        var studentToken = await _factory.GetTokenAsync("matrix-student-a");
        const string script = "<script>alert(1)</script>";

        using var response = await SendAsync(
            HttpMethod.Post,
            "/api/me/placement-requests",
            studentToken,
            JsonContent.Create(new
            {
                companyId = _factory.CompanyBId,
                jobPositionId = _factory.CompanyBPositionId,
                applicantFullName = "Student A",
                applicantEmail = "a@example.test",
                applicantPhone = "0900000000",
                applicantSchool = "Test University",
                applicantMajor = "Information Technology",
                cvUrl = "https://example.test/a-cv.pdf",
                coverLetter = script,
                note = script
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        AssertNoSecretProperties(document.RootElement, "POST /api/me/placement-requests");
        var data = document.RootElement.GetProperty("data");
        Assert.Equal(script, data.GetProperty("coverLetter").GetString());
        Assert.Equal(script, data.GetProperty("note").GetString());
    }

    [Fact]
    public async Task Admin_who_must_change_password_cannot_use_management_endpoints()
    {
        using var factory = new AuthorizationApiFactory();
        await factory.EnsureSeededAsync();
        await factory.RequireAdminPasswordChangeAsync();
        var token = await factory.GetTokenAsync("matrix-admin");

        using var response = await SendWithClientAsync(
            factory.Client,
            HttpMethod.Get,
            "/api/students",
            token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_endpoint_keeps_script_markup_as_plain_json_text()
    {
        await _factory.EnsureSeededAsync();
        var adminToken = await _factory.GetTokenAsync("matrix-admin");
        const string script = "<script>alert(1)</script>";

        using var response = await SendAsync(
            HttpMethod.Post,
            "/api/students",
            adminToken,
            JsonContent.Create(new
            {
                studentCode = "SCRIPT-001",
                fullName = script,
                email = "script-student@example.test",
                phoneNumber = "0900000001",
                major = "Information Technology",
                className = "IT-01"
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        AssertNoSecretProperties(document.RootElement, "POST /api/students");
        Assert.Equal(script, document.RootElement.GetProperty("data").GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task Company_endpoint_keeps_script_markup_as_plain_json_text()
    {
        await _factory.EnsureSeededAsync();
        var adminToken = await _factory.GetTokenAsync("matrix-admin");
        const string script = "<script>alert(1)</script>";

        using var response = await SendAsync(
            HttpMethod.Post,
            "/api/companies",
            adminToken,
            JsonContent.Create(new
            {
                name = script,
                address = "Test address",
                industry = "Technology",
                contactPerson = "Contact",
                contactPhone = "0900000001",
                contactEmail = "script-company@example.test",
                contactPosition = "Recruiter"
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        AssertNoSecretProperties(document.RootElement, "POST /api/companies");
        Assert.Equal(script, document.RootElement.GetProperty("data").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Job_position_endpoint_keeps_script_markup_as_plain_json_text()
    {
        await _factory.EnsureSeededAsync();
        var adminToken = await _factory.GetTokenAsync("matrix-admin");
        const string script = "<script>alert(1)</script>";

        using var response = await SendAsync(
            HttpMethod.Post,
            "/api/job-positions",
            adminToken,
            JsonContent.Create(new
            {
                companyId = _factory.CompanyAId,
                title = script,
                department = "Engineering",
                location = "Hà Nội",
                quantity = 1,
                description = "A valid test position description",
                isOpen = true
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        AssertNoSecretProperties(document.RootElement, "POST /api/job-positions");
        Assert.Equal(script, document.RootElement.GetProperty("data").GetProperty("title").GetString());
    }

    [Fact]
    public async Task About_endpoint_keeps_script_markup_as_plain_json_text()
    {
        await _factory.EnsureSeededAsync();
        var adminToken = await _factory.GetTokenAsync("matrix-admin");
        const string script = "<script>alert(1)</script>";
        var content = AboutContentDefaults.CreateDefault();
        content.Name = script;

        using var response = await SendAsync(
            HttpMethod.Put,
            "/api/about",
            adminToken,
            JsonContent.Create(content));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        AssertNoSecretProperties(document.RootElement, "PUT /api/about");
        Assert.Equal(
            script,
            document.RootElement.GetProperty("data").GetProperty("content").GetProperty("name").GetString());
    }

    private async Task AssertDeniedForRoleAsync(
        IReadOnlyCollection<RouteEndpoint> endpoints,
        string role,
        string token)
    {
        var restrictedEndpoints = endpoints.Where(endpoint => !IsRoleAllowed(endpoint, role)).ToArray();
        Assert.NotEmpty(restrictedEndpoints);

        foreach (var endpoint in restrictedEndpoints)
        {
            using var response = await SendEndpointAsync(endpoint, token);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    private IReadOnlyCollection<RouteEndpoint> GetControllerEndpoints()
        => GetControllerEndpoints(_factory);

    private static IReadOnlyCollection<RouteEndpoint> GetControllerEndpoints(AuthorizationApiFactory factory)
    {
        var endpoints = factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
            .ToArray();

        Assert.NotEmpty(endpoints);
        return endpoints;
    }

    private Task<HttpResponseMessage> SendEndpointAsync(RouteEndpoint endpoint, string? token = null)
    {
        var method = new HttpMethod(GetMethod(endpoint));
        return SendAsync(method, GetPath(endpoint), token);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        string? token = null,
        HttpContent? content = null)
    {
        return await SendWithClientAsync(_factory.Client, method, path, token, content);
    }

    private static async Task<HttpResponseMessage> SendWithClientAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string? token = null,
        HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await client.SendAsync(request);
    }

    private static string GetMethod(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.FirstOrDefault() ?? "GET";

    private static string GetPath(RouteEndpoint endpoint)
    {
        var rawPath = endpoint.RoutePattern.RawText
            ?? throw new InvalidOperationException("Controller endpoint is missing a route pattern.");
        var path = Regex.Replace(rawPath, @"\{[^}]+\}", "1");
        return $"/{path.Trim('/')}";
    }

    private static string GetPathWithUnmatchedRouteIds(RouteEndpoint endpoint)
    {
        var rawPath = endpoint.RoutePattern.RawText
            ?? throw new InvalidOperationException("Controller endpoint is missing a route pattern.");
        var path = Regex.Replace(rawPath, @"\{[^}]+\}", int.MaxValue.ToString());
        return $"/{path.Trim('/')}";
    }

    private static string ToEndpointKey(RouteEndpoint endpoint) =>
        $"{GetMethod(endpoint).ToUpperInvariant()} {GetPath(endpoint).ToLowerInvariant()}";

    private static bool IsRoleAllowed(RouteEndpoint endpoint, string role)
    {
        var roleRequirements = endpoint.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .Where(metadata => !string.IsNullOrWhiteSpace(metadata.Roles));

        return roleRequirements.All(metadata =>
            metadata.Roles!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    private static string SelectAllowedRole(RouteEndpoint endpoint)
    {
        foreach (var role in new[] { "Admin", "User", "Company" })
        {
            if (IsRoleAllowed(endpoint, role))
            {
                return role;
            }
        }

        throw new InvalidOperationException($"No supported role is allowed for {ToEndpointKey(endpoint)}.");
    }

    private static void AssertNoSecretProperties(JsonElement element, string endpoint)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                Assert.False(
                    property.Name.Equals("passwordHash", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("token", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("refreshToken", StringComparison.OrdinalIgnoreCase),
                    $"Sensitive property '{property.Name}' was returned by {endpoint}.");
                AssertNoSecretProperties(property.Value, endpoint);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                AssertNoSecretProperties(item, endpoint);
            }
        }
    }
}

public sealed class AuthorizationApiFactory : WebApplicationFactory<Program>
{
    private const string TestJwtSecret = "authorization-matrix-test-secret-key-at-least-32-characters";
    private const string TestPassword = "MatrixPass2026";
    private readonly string _databaseName = $"AuthorizationMatrix-{Guid.NewGuid():N}";
    private readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.Ordinal);
    private Task? _seedTask;

    public HttpClient Client { get; private set; } = null!;
    public int CompanyBRequestId { get; private set; }
    public int CompanyBPositionId { get; private set; }
    public int StudentBRequestId { get; private set; }
    public int CompanyAId { get; private set; }
    public int CompanyAPositionId { get; private set; }
    public int CompanyBId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Jwt:SecretKey", TestJwtSecret);
        builder.UseSetting("Jwt:Issuer", "authorization-matrix-tests");
        builder.UseSetting("Jwt:Audience", "authorization-matrix-tests");
        builder.UseSetting("Jwt:AccessTokenExpiryMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenExpiryDays", "7");
        builder.UseSetting("PlacementRequestExpiry:Enabled", "false");
        builder.UseSetting("Smtp:Enabled", "false");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = TestJwtSecret,
                ["Jwt:Issuer"] = "authorization-matrix-tests",
                ["Jwt:Audience"] = "authorization-matrix-tests",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "7",
                ["PlacementRequestExpiry:Enabled"] = "false",
                ["Smtp:Enabled"] = "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    public Task EnsureSeededAsync()
    {
        lock (this)
        {
            Client ??= CreateClient();
            _seedTask ??= SeedAsync();
            return _seedTask;
        }
    }

    public async Task<string> GetTokenAsync(string username)
    {
        await EnsureSeededAsync();
        if (_tokens.TryGetValue(username, out var cachedToken))
        {
            return cachedToken;
        }

        using var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password = TestPassword
        });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty("data").GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Login response did not contain an access token.");
        return _tokens.GetOrAdd(username, token);
    }

    public async Task RequireAdminPasswordChangeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var admin = await db.Users.SingleAsync(user => user.Username == "matrix-admin");
        admin.MustChangePassword = true;
        await db.SaveChangesAsync();
        _tokens.TryRemove("matrix-admin", out _);
    }

    private async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var companyA = new Company
        {
            Name = "Matrix Company A",
            Address = "A Street",
            Industry = "Technology",
            ContactPerson = "Company Contact A",
            ContactEmail = "company-a@example.test",
            ContactPhone = "0900000001"
        };
        var companyB = new Company
        {
            Name = "Matrix Company B",
            Address = "B Street",
            Industry = "Technology",
            ContactPerson = "Company Contact B",
            ContactEmail = "company-b@example.test",
            ContactPhone = "0900000002"
        };
        db.Companies.AddRange(companyA, companyB);
        await db.SaveChangesAsync();
        CompanyAId = companyA.Id;
        CompanyBId = companyB.Id;

        var studentA = CreateStudent("A001", "Student A", "a@example.test");
        var studentB = CreateStudent("B001", "Student B", "b@example.test");
        db.Students.AddRange(studentA, studentB);
        await db.SaveChangesAsync();

        var positionA = CreatePosition(companyA.Id, "Position A");
        var positionB = CreatePosition(companyB.Id, "Position B");
        db.JobPositions.AddRange(positionA, positionB);
        await db.SaveChangesAsync();
        CompanyAPositionId = positionA.Id;
        CompanyBPositionId = positionB.Id;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(TestPassword);
        db.Users.AddRange(
            CreateUser("matrix-admin", passwordHash, UserRole.Admin),
            CreateUser("matrix-student-a", passwordHash, UserRole.User, studentId: studentA.Id),
            CreateUser("matrix-student-b", passwordHash, UserRole.User, studentId: studentB.Id),
            CreateUser("matrix-company-a", passwordHash, UserRole.Company, companyId: companyA.Id),
            CreateUser("matrix-company-b", passwordHash, UserRole.Company, companyId: companyB.Id));
        await db.SaveChangesAsync();

        db.PlacementRequests.AddRange(
            CreateRequest(studentA.Id, companyA.Id, positionA.Id, "Student A"),
            CreateRequest(studentB.Id, companyB.Id, positionB.Id, "Student B"));
        await db.SaveChangesAsync();

        CompanyBRequestId = await db.PlacementRequests
            .Where(request => request.StudentId == studentB.Id)
            .Select(request => request.Id)
            .SingleAsync();
        StudentBRequestId = CompanyBRequestId;
    }

    private static Student CreateStudent(string code, string name, string email) => new()
    {
        StudentCode = code,
        FullName = name,
        Major = "Information Technology",
        ClassName = "IT-01",
        Email = email,
        PhoneNumber = "0900000000",
        Status = StudentStatus.NoCompany,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static JobPosition CreatePosition(int companyId, string title) => new()
    {
        CompanyId = companyId,
        Title = title,
        Department = "Engineering",
        Location = "Hà Nội",
        Quantity = 1,
        Description = "Internship role",
        IsOpen = true,
        ApprovalStatus = JobPositionApprovalStatus.Approved
    };

    private static User CreateUser(
        string username,
        string passwordHash,
        UserRole role,
        int? studentId = null,
        int? companyId = null) => new()
    {
        Username = username,
        PasswordHash = passwordHash,
        Role = role,
        IsActive = true,
        MustChangePassword = false,
        StudentId = studentId,
        CompanyId = companyId,
        CreatedAt = DateTime.UtcNow
    };

    private static PlacementRequest CreateRequest(
        int studentId,
        int companyId,
        int positionId,
        string applicantName) => new()
    {
        StudentId = studentId,
        CompanyId = companyId,
        JobPositionId = positionId,
        ApplicantFullName = applicantName,
        ApplicantEmail = $"{applicantName.Replace(" ", "-").ToLowerInvariant()}@example.test",
        ApplicantPhone = "0900000000",
        ApplicantSchool = "Test University",
        ApplicantMajor = "Information Technology",
        CvUrl = "https://example.test/cv.pdf",
        CoverLetter = "Application",
        Status = RequestStatus.Pending,
        CreatedAt = DateTime.UtcNow,
        DueAt = DateTime.UtcNow.AddDays(7)
    };
}
