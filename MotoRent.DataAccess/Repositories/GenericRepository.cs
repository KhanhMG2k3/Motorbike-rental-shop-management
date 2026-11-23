using Microsoft.EntityFrameworkCore;

namespace MotoRent.DataAccess.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly MotorbikeRentalContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(MotorbikeRentalContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public IQueryable<T> Query() => _dbSet.AsNoTracking();

    public IQueryable<T> QueryTracking() => _dbSet;

    public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public void Update(T entity) => _dbSet.Update(entity);

    public void Remove(T entity) => _dbSet.Remove(entity);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
