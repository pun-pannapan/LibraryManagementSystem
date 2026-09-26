using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryManagement.Application.Contracts;
using LibraryManagement.Domain.Enums;

namespace LibraryManagement.Api.Tests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task Login_ReturnsToken_ForSeededUser()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("user@example.com", "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.User.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task GetBooks_ReturnsPagedSeededBooks()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, "user@example.com");

        var response = await client.GetAsync("/api/v1/books?search=Clean&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<BookDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().ContainSingle(book => book.Title == "Clean Code");
        body.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task AnonymousClientCannotReadBooksOrCategories()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        using var client = factory.CreateClient();

        var booksResponse = await client.GetAsync("/api/v1/books");
        var bookResponse = await client.GetAsync($"/api/v1/books/{TestApplicationFactory.CleanCodeBookId}");
        var categoriesResponse = await client.GetAsync("/api/v1/categories");

        booksResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        bookResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        categoriesResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminCanCreateBook()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, "admin@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/v1/books",
            new CreateBookRequest(
                "9780321125217",
                "Domain-Driven Design",
                "Eric Evans",
                "Addison-Wesley",
                2003,
                TestApplicationFactory.TechnologyCategoryId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<BookDto>();
        body.Should().NotBeNull();
        body!.Title.Should().Be("Domain-Driven Design");
    }

    [Fact]
    public async Task AdministratorCannotBorrowOrViewPersonalBorrowingHistory()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, "admin@example.com");

        var borrowResponse = await client.PostAsJsonAsync(
            "/api/v1/borrowings",
            new BorrowBookRequest(TestApplicationFactory.CleanCodeBookId));
        var historyResponse = await client.GetAsync("/api/v1/borrowings/me");

        borrowResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        historyResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserCanRequestBorrowAndReturnWithAdministratorProcessing()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, "user@example.com");

        var borrowResponse = await client.PostAsJsonAsync("/api/v1/borrowings", new BorrowBookRequest(TestApplicationFactory.CleanCodeBookId));

        borrowResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var borrowing = await borrowResponse.Content.ReadFromJsonAsync<BorrowTransactionDto>();
        borrowing.Should().NotBeNull();

        borrowing!.Status.Should().Be(LibraryManagement.Domain.Enums.BorrowTransactionStatus.Requested);

        await AuthorizeAsync(client, "admin@example.com");
        var assignResponse = await client.PostAsync($"/api/v1/borrowings/{borrowing.Id}/assign", null);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await AuthorizeAsync(client, "user@example.com");
        var returnResponse = await client.PostAsync($"/api/v1/borrowings/{borrowing.Id}/return", null);

        returnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnRequest = await returnResponse.Content.ReadFromJsonAsync<BorrowTransactionDto>();
        returnRequest.Should().NotBeNull();
        returnRequest!.Status.Should().Be(LibraryManagement.Domain.Enums.BorrowTransactionStatus.ReturnRequested);

        await AuthorizeAsync(client, "admin@example.com");
        var acceptedResponse = await client.PostAsync($"/api/v1/borrowings/{borrowing.Id}/accept-return", null);
        acceptedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var returned = await acceptedResponse.Content.ReadFromJsonAsync<BorrowTransactionDto>();
        returned.Should().NotBeNull();
        returned!.ReturnedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task AdminHistorySearchesByBookTitle()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, "user@example.com");

        var borrowResponse = await client.PostAsJsonAsync(
            "/api/v1/borrowings",
            new BorrowBookRequest(TestApplicationFactory.CleanCodeBookId));
        borrowResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        await AuthorizeAsync(client, "admin@example.com");
        var searchResponse = await client.GetAsync("/api/v1/borrowings?search=Clean%20Code&page=1&pageSize=20");

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await searchResponse.Content.ReadFromJsonAsync<PagedResult<BorrowTransactionDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().ContainSingle(item => item.BookTitle == "Clean Code");
    }

    [Fact]
    public async Task UserCannotProcessAdministratorBorrowingActions()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, "user@example.com");
        var borrowing = await CreateBorrowRequestAsync(client);

        var assignResponse = await client.PostAsync($"/api/v1/borrowings/{borrowing.Id}/assign", null);
        var rejectResponse = await client.PostAsync($"/api/v1/borrowings/{borrowing.Id}/reject", null);
        var acceptReturnResponse = await client.PostAsync($"/api/v1/borrowings/{borrowing.Id}/accept-return", null);

        assignResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        rejectResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        acceptReturnResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CancellingPendingRequestReleasesBook()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, "user@example.com");
        var borrowing = await CreateBorrowRequestAsync(client);

        var cancelResponse = await client.PostAsync($"/api/v1/borrowings/{borrowing.Id}/cancel", null);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<BorrowTransactionDto>();
        cancelled!.Status.Should().Be(BorrowTransactionStatus.Cancelled);
        var book = await client.GetFromJsonAsync<BookDto>($"/api/v1/books/{TestApplicationFactory.CleanCodeBookId}");
        book!.AvailabilityStatus.Should().Be(BookAvailabilityStatus.Available);
    }

    [Fact]
    public async Task RejectingPendingRequestReleasesBook()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, "user@example.com");
        var borrowing = await CreateBorrowRequestAsync(client);
        await AuthorizeAsync(client, "admin@example.com");

        var rejectResponse = await client.PostAsync($"/api/v1/borrowings/{borrowing.Id}/reject", null);

        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rejected = await rejectResponse.Content.ReadFromJsonAsync<BorrowTransactionDto>();
        rejected!.Status.Should().Be(BorrowTransactionStatus.Rejected);
        var book = await client.GetFromJsonAsync<BookDto>($"/api/v1/books/{TestApplicationFactory.CleanCodeBookId}");
        book!.AvailabilityStatus.Should().Be(BookAvailabilityStatus.Available);
    }

    [Fact]
    public async Task ReturnCannotBeAcceptedBeforeUserRequestsIt()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, "user@example.com");
        var borrowing = await CreateBorrowRequestAsync(client);
        await AuthorizeAsync(client, "admin@example.com");
        var assignResponse = await client.PostAsync($"/api/v1/borrowings/{borrowing.Id}/assign", null);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var acceptReturnResponse = await client.PostAsync($"/api/v1/borrowings/{borrowing.Id}/accept-return", null);

        acceptReturnResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task BookWithPendingRequestCannotBeRequestedAgain()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, "user@example.com");
        await CreateBorrowRequestAsync(client);

        var duplicateResponse = await client.PostAsJsonAsync(
            "/api/v1/borrowings",
            new BorrowBookRequest(TestApplicationFactory.CleanCodeBookId));

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UserCannotReturnAnotherUsersBorrowing()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, "user@example.com");
        var borrowing = await CreateBorrowRequestAsync(client);
        await AuthorizeAsync(client, "admin@example.com");
        var assignResponse = await client.PostAsync($"/api/v1/borrowings/{borrowing.Id}/assign", null);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await AuthorizeAsync(client, "other@example.com");

        var returnResponse = await client.PostAsync($"/api/v1/borrowings/{borrowing.Id}/return", null);

        returnResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<BorrowTransactionDto> CreateBorrowRequestAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/borrowings",
            new BorrowBookRequest(TestApplicationFactory.CleanCodeBookId));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<BorrowTransactionDto>())!;
    }

    private static async Task AuthorizeAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Password123!"));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }
}
