namespace MotoRent.BusinessObjects.Exceptions;

/// <summary>Lỗi nghiệp vụ → HTTP 400.</summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}

/// <summary>Không tìm thấy dữ liệu → HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Trùng dữ liệu / xung đột trạng thái → HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
