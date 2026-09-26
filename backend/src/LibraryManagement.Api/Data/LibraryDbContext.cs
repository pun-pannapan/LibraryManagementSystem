using LibraryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Data;

// Compatibility wrapper while the remaining API wiring moves to Infrastructure.
public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options)
    : ApplicationDbContext(options);
