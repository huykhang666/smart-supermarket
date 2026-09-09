using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Products.DTOs;
using SmartSupermarket.Backend.Features.Products.Repositories;

namespace SmartSupermarket.Backend.Features.Products.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;

    public SupplierService(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<SupplierDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        return supplier == null ? null : MapToDto(supplier);
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var suppliers = await _supplierRepository.GetAllAsync(cancellationToken);
        return suppliers.Select(MapToDto);
    }

    public async Task<SupplierPagedResult> GetPagedAsync(string? search, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _supplierRepository.GetPagedAsync(search, page, pageSize, cancellationToken);

        return new SupplierPagedResult
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SupplierName))
        {
            throw new ArgumentException("Tên nhà cung cấp không được để trống.");
        }

        string name = request.SupplierName.Trim();
        string? phone = request.PhoneNumber?.Trim();
        string? email = request.Email?.Trim();

        if (await _supplierRepository.ExistsPhoneOrEmailAsync(phone, email, null, cancellationToken))
        {
            throw new InvalidOperationException("Số điện thoại hoặc Email của Nhà cung cấp này đã bị trùng trên hệ thống.");
        }

        var supplier = new Supplier
        {
            SupplierName = name,
            ContactPerson = request.ContactPerson?.Trim(),
            PhoneNumber = phone,
            Email = email,
            Address = request.Address?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _supplierRepository.AddAsync(supplier, cancellationToken);
        await _supplierRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(int id, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhà cung cấp với ID {id}.");
        }

        if (string.IsNullOrWhiteSpace(request.SupplierName))
        {
            throw new ArgumentException("Tên nhà cung cấp không được để trống.");
        }

        string name = request.SupplierName.Trim();
        string? phone = request.PhoneNumber?.Trim();
        string? email = request.Email?.Trim();

        if (await _supplierRepository.ExistsPhoneOrEmailAsync(phone, email, id, cancellationToken))
        {
            throw new InvalidOperationException("Số điện thoại hoặc Email đã được sử dụng bởi Nhà cung cấp khác.");
        }

        supplier.SupplierName = name;
        supplier.ContactPerson = request.ContactPerson?.Trim();
        supplier.PhoneNumber = phone;
        supplier.Email = email;
        supplier.Address = request.Address?.Trim();

        _supplierRepository.Update(supplier);
        await _supplierRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(supplier);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhà cung cấp với ID {id}.");
        }

        _supplierRepository.Delete(supplier);
        await _supplierRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto
        {
            SupplierId = supplier.SupplierId,
            SupplierName = supplier.SupplierName,
            ContactPerson = supplier.ContactPerson,
            PhoneNumber = supplier.PhoneNumber,
            Email = supplier.Email,
            Address = supplier.Address,
            ProductCount = supplier.Products?.Count ?? 0,
            CreatedAt = supplier.CreatedAt
        };
    }
}
