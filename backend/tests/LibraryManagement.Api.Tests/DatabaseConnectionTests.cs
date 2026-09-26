using LibraryManagement.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace LibraryManagement.Api.Tests;

public class DatabaseConnectionTests
{
    [Theory]
    [InlineData("Demo;Password123!")]
    [InlineData("Demo\"Password'123!")]
    [InlineData(" leading and trailing spaces ")]
    [InlineData("Demo;Encrypt=True;Password=other")]
    public void SpecialCharactersRemainPartOfThePassword(string password)
    {
        var result = new SqlConnectionStringBuilder(DatabaseConnection.Create(Config(Settings(password))));
        Assert.Equal(password, result.Password);
        Assert.Equal("db,1433", result.DataSource);
        Assert.Equal("LibraryManagementDb", result.InitialCatalog);
        Assert.Equal("sa", result.UserID);
        Assert.Equal(SqlConnectionEncryptOption.Optional, result.Encrypt);
        Assert.Equal(3, result.ConnectTimeout);
    }

    [Theory]
    [InlineData("Database:Server")]
    [InlineData("Database:Name")]
    [InlineData("Database:User")]
    [InlineData("Database:Password")]
    public void MissingSettingsFailBeforeOpeningAConnection(string key)
    {
        var settings = Settings("TestPassword123!");
        settings.Remove(key);
        var exception = Assert.Throws<InvalidOperationException>(
            () => DatabaseConnection.Create(Config(settings)));
        Assert.Contains(key, exception.Message);
        Assert.DoesNotContain("TestPassword123!", exception.Message);
    }

    [Fact]
    public void ExplicitConnectionStringRemainsSupportedForExistingTools()
    {
        var settings = Settings("TestPassword123!");
        settings["ConnectionStrings:DefaultConnection"] =
            "Server=localhost,1435;Database=ToolDb;Integrated Security=True;Encrypt=True";
        var result = new SqlConnectionStringBuilder(DatabaseConnection.Create(Config(settings)));
        Assert.Equal("localhost,1435", result.DataSource);
        Assert.Equal("ToolDb", result.InitialCatalog);
        Assert.True(result.IntegratedSecurity);
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, result.Encrypt);
    }

    private static Dictionary<string, string?> Settings(string password) => new()
    {
        ["Database:Server"] = "db,1433",
        ["Database:Name"] = "LibraryManagementDb",
        ["Database:User"] = "sa",
        ["Database:Password"] = password
    };

    private static IConfiguration Config(Dictionary<string, string?> settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
}
