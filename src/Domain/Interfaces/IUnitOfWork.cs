using Microsoft.EntityFrameworkCore.Storage;

namespace POS.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProductInterface Products { get; }
    IReceiptInterface Receipts { get; }
    ITransactionInterface Transactions { get; }
    ICategoryInterface Categories { get; }
    IProductItemInterface ProductItems { get; }
    IUserInterface Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
