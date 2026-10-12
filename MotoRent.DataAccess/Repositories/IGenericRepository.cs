namespace MotoRent.DataAccess.Repositories;

public interface IGenericRepository<T> where T : class
{
    /// <summary>Truy vấn chỉ đọc (AsNoTracking) — dùng cho GET / OData.</summary>
    IQueryable<T> Query();

    /// <summary>Truy vấn có tracking — dùng khi cần sửa entity rồi SaveChanges.</summary>
    IQueryable<T> QueryTracking();

    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync();
}
