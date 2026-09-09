using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Features.Suppliers.DTOs;
using SmartSupermarket.Backend.Features.Suppliers.Repositories;

namespace SmartSupermarket.Backend.Features.Suppliers.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductSupplierRepository _productSupplierRepository;

    public SupplierService(
        ISupplierRepository supplierRepository,
        IProductSupplierRepository productSupplierRepository)
    {
        _supplierRepository = supplierRepository;
        _productSupplierRepository = productSupplierRepository;
    }

    public async Task<SupplierDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        return supplier == null ? null : MapToDto(supplier);
    }

    public async Task<SupplierDto?> GetByCodeAsync(string supplierCode, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByCodeAsync(supplierCode, cancellationToken);
        return supplier == null ? null : MapToDto(supplier);
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var suppliers = await _supplierRepository.GetAllAsync(includeInactive, cancellationToken);
        return suppliers.Select(MapToDto);
    }

    public async Task<SupplierPagedResult> GetPagedAsync(
        string? search,
        byte? status,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _supplierRepository.GetPagedAsync(search, status, page, pageSize, cancellationToken);

        return new SupplierPagedResult
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<SupplierDto>> SearchAsync(string search, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var suppliers = await _supplierRepository.SearchAsync(search, includeInactive, cancellationToken);
        return suppliers.Select(MapToDto);
    }

    public async Task<IEnumerable<SupplierDropdownDto>> GetDropdownAsync(CancellationToken cancellationToken = default)
    {
        var suppliers = await _supplierRepository.GetDropdownAsync(cancellationToken);
        return suppliers.Select(s => new SupplierDropdownDto
        {
            SupplierId = s.SupplierId,
            SupplierCode = s.SupplierCode,
            SupplierName = s.SupplierName
        });
    }

    public async Task<IEnumerable<ProductSupplierDto>> GetSuppliedProductsBySupplierIdAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhà cung cấp có ID = {supplierId}.");
        }

        var links = await _productSupplierRepository.GetSuppliedProductsBySupplierIdAsync(supplierId, cancellationToken);
        return links.Select(MapToProductSupplierDto);
    }

    public async Task<SupplierStatisticsDto> GetSupplierStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var allSuppliers = (await _supplierRepository.GetAllAsync(includeInactive: true, cancellationToken)).ToList();
        int total = allSuppliers.Count;
        int active = allSuppliers.Count(s => s.Status == 1);
        int inactive = allSuppliers.Count(s => s.Status == 2);
        int totalSuppliedProducts = allSuppliers.Sum(s => s.ProductSuppliers?.Count ?? s.Products?.Count ?? 0);

        var allLinks = new List<ProductSupplier>();
        foreach (var s in allSuppliers)
        {
            if (s.ProductSuppliers != null)
            {
                allLinks.AddRange(s.ProductSuppliers);
            }
        }

        decimal avgRating = allLinks.Any() ? Math.Round(allLinks.Average(ps => ps.Rating), 2) : 5.00m;

        return new SupplierStatisticsDto
        {
            TotalSuppliers = total,
            ActiveSuppliers = active,
            InactiveSuppliers = inactive,
            TotalSuppliedProducts = totalSuppliedProducts,
            AverageRating = avgRating
        };
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        ValidateSupplierRequest(request.SupplierName, request.PhoneNumber, request.Email);

        string name = request.SupplierName.Trim();

        // BR-SUPP-01: Unique Name
        if (await _supplierRepository.ExistsNameAsync(name, null, cancellationToken))
        {
            throw new InvalidOperationException($"Nhà cung cấp tên '{name}' đã tồn tại trên hệ thống (BR-SUPP-01).");
        }

        // Code auto-generation if not provided
        string code = string.IsNullOrWhiteSpace(request.SupplierCode)
            ? $"SUP{DateTime.UtcNow.Ticks % 100000:D5}"
            : request.SupplierCode.Trim().ToUpper();

        if (await _supplierRepository.ExistsCodeAsync(code, null, cancellationToken))
        {
            code = $"SUP{DateTime.UtcNow.Ticks % 100000:D5}";
        }

        var supplier = new Supplier
        {
            SupplierCode = code,
            SupplierName = name,
            ContactPerson = request.ContactPerson?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            TaxCode = request.TaxCode?.Trim(),
            LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? "/images/suppliers/no-logo.png" : request.LogoUrl.Trim(),
            Status = request.Status == 0 ? (byte)1 : request.Status,
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
            throw new KeyNotFoundException($"Không tìm thấy nhà cung cấp có ID = {id}.");
        }

        ValidateSupplierRequest(request.SupplierName, request.PhoneNumber, request.Email);

        string name = request.SupplierName.Trim();

        // BR-SUPP-01: Unique Name
        if (await _supplierRepository.ExistsNameAsync(name, id, cancellationToken))
        {
            throw new InvalidOperationException($"Tên nhà cung cấp '{name}' đã được sử dụng bởi nhà cung cấp khác (BR-SUPP-01).");
        }

        supplier.SupplierName = name;
        supplier.ContactPerson = request.ContactPerson?.Trim();
        supplier.PhoneNumber = request.PhoneNumber?.Trim();
        supplier.Email = request.Email?.Trim();
        supplier.Address = request.Address?.Trim();
        supplier.TaxCode = request.TaxCode?.Trim();
        if (!string.IsNullOrWhiteSpace(request.LogoUrl))
        {
            supplier.LogoUrl = request.LogoUrl.Trim();
        }
        supplier.Status = request.Status;
        supplier.UpdatedAt = DateTime.UtcNow;

        _supplierRepository.Update(supplier);
        await _supplierRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(supplier);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await SoftDeleteAsync(id, cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhà cung cấp có ID = {id}.");
        }

        supplier.Status = 2; // Inactive / Soft Deleted
        supplier.UpdatedAt = DateTime.UtcNow;

        _supplierRepository.Update(supplier);
        await _supplierRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<SupplierDto> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhà cung cấp có ID = {id}.");
        }

        supplier.Status = 1; // Active
        supplier.UpdatedAt = DateTime.UtcNow;

        _supplierRepository.Update(supplier);
        await _supplierRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(supplier);
    }

    public async Task<string> UploadLogoAsync(int id, string logoUrl, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhà cung cấp có ID = {id}.");
        }

        supplier.LogoUrl = logoUrl;
        supplier.UpdatedAt = DateTime.UtcNow;

        _supplierRepository.Update(supplier);
        await _supplierRepository.SaveChangesAsync(cancellationToken);

        return supplier.LogoUrl;
    }

    public async Task<bool> LinkProductSupplierAsync(LinkProductSupplierRequest request, CancellationToken cancellationToken = default)
    {
        ValidateLinkRequest(request.PurchasePrice, request.LeadTime, request.MinimumOrderQuantity, request.Rating);

        var existingLink = await _productSupplierRepository.GetLinkAsync(request.ProductId, request.SupplierId, cancellationToken);
        if (existingLink != null)
        {
            // Update existing link
            existingLink.PurchasePrice = request.PurchasePrice;
            existingLink.SupplierProductCode = request.SupplierProductCode;
            existingLink.LeadTime = request.LeadTime;
            existingLink.MinimumOrderQuantity = request.MinimumOrderQuantity;
            existingLink.Rating = request.Rating;
            existingLink.IsDefault = request.IsDefault;
            existingLink.UpdatedAt = DateTime.UtcNow;

            if (request.IsDefault)
            {
                await _productSupplierRepository.ResetOtherDefaultsAsync(request.ProductId, request.SupplierId, cancellationToken);
            }

            _productSupplierRepository.UpdateLink(existingLink);
        }
        else
        {
            // Add new link
            var link = new ProductSupplier
            {
                ProductId = request.ProductId,
                SupplierId = request.SupplierId,
                PurchasePrice = request.PurchasePrice,
                SupplierProductCode = request.SupplierProductCode,
                LeadTime = request.LeadTime,
                MinimumOrderQuantity = request.MinimumOrderQuantity,
                Rating = request.Rating,
                IsDefault = request.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            if (request.IsDefault)
            {
                await _productSupplierRepository.ResetOtherDefaultsAsync(request.ProductId, request.SupplierId, cancellationToken);
            }

            await _productSupplierRepository.AddLinkAsync(link, cancellationToken);
        }

        await _productSupplierRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnlinkProductSupplierAsync(int productId, int supplierId, CancellationToken cancellationToken = default)
    {
        var link = await _productSupplierRepository.GetLinkAsync(productId, supplierId, cancellationToken);
        if (link == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy liên kết cung ứng giữa sản phẩm ID {productId} và nhà cung cấp ID {supplierId}.");
        }

        _productSupplierRepository.Unlink(link);
        await _productSupplierRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateProductSupplierLinkAsync(int productId, int supplierId, UpdateLinkRequest request, CancellationToken cancellationToken = default)
    {
        ValidateLinkRequest(request.PurchasePrice, request.LeadTime, request.MinimumOrderQuantity, request.Rating);

        var link = await _productSupplierRepository.GetLinkAsync(productId, supplierId, cancellationToken);
        if (link == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy liên kết cung ứng giữa sản phẩm ID {productId} và nhà cung cấp ID {supplierId}.");
        }

        link.PurchasePrice = request.PurchasePrice;
        link.SupplierProductCode = request.SupplierProductCode;
        link.LeadTime = request.LeadTime;
        link.MinimumOrderQuantity = request.MinimumOrderQuantity;
        link.Rating = request.Rating;
        link.IsDefault = request.IsDefault;
        link.UpdatedAt = DateTime.UtcNow;

        if (request.IsDefault)
        {
            await _productSupplierRepository.ResetOtherDefaultsAsync(productId, supplierId, cancellationToken);
        }

        _productSupplierRepository.UpdateLink(link);
        await _productSupplierRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ImportSupplierResultDto> ImportFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default)
    {
        var result = new ImportSupplierResultDto();
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            result.Errors.Add("Nội dung JSON rỗng.");
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;
            var items = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray() : new[] { root }.AsEnumerable();

            foreach (var item in items)
            {
                result.TotalRows++;
                try
                {
                    string name = item.GetProperty("supplierName").GetString() ?? string.Empty;
                    string? code = item.TryGetProperty("supplierCode", out var pCode) ? pCode.GetString() : null;
                    string? contact = item.TryGetProperty("contactPerson", out var pContact) ? pContact.GetString() : null;
                    string? phone = item.TryGetProperty("phoneNumber", out var pPhone) ? pPhone.GetString() : null;
                    string? email = item.TryGetProperty("email", out var pEmail) ? pEmail.GetString() : null;
                    string? address = item.TryGetProperty("address", out var pAddress) ? pAddress.GetString() : null;

                    await CreateAsync(new CreateSupplierRequest
                    {
                        SupplierCode = code,
                        SupplierName = name,
                        ContactPerson = contact,
                        PhoneNumber = phone,
                        Email = email,
                        Address = address
                    }, cancellationToken);

                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Dòng {result.TotalRows}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Lỗi định dạng JSON: {ex.Message}");
        }

        return result;
    }

    public async Task<ImportSupplierResultDto> ImportFromCsvAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        var result = new ImportSupplierResultDto();
        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        string? headerLine = await reader.ReadLineAsync(cancellationToken);
        if (headerLine == null)
        {
            result.Errors.Add("File CSV rỗng.");
            return result;
        }

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            result.TotalRows++;
            var parts = line.Split(',');
            if (parts.Length < 1) continue;

            try
            {
                string name = parts[0].Trim();
                string? code = parts.Length > 1 ? parts[1].Trim() : null;
                string? phone = parts.Length > 2 ? parts[2].Trim() : null;
                string? email = parts.Length > 3 ? parts[3].Trim() : null;

                await CreateAsync(new CreateSupplierRequest
                {
                    SupplierName = name,
                    SupplierCode = code,
                    PhoneNumber = phone,
                    Email = email
                }, cancellationToken);

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Errors.Add($"Dòng {result.TotalRows}: {ex.Message}");
            }
        }

        return result;
    }

    private static void ValidateSupplierRequest(string supplierName, string? phoneNumber, string? email)
    {
        if (string.IsNullOrWhiteSpace(supplierName))
        {
            throw new ArgumentException("Tên nhà cung cấp không được để trống (BR-SUPP-01).");
        }

        if (supplierName.Trim().Length > 150)
        {
            throw new ArgumentException("Tên nhà cung cấp không được vượt quá 150 ký tự (BR-SUPP-01).");
        }

        // BR-SUPP-02: Phone validation
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var phoneTrim = phoneNumber.Trim();
            if (!Regex.IsMatch(phoneTrim, @"^0\d{9,10}$"))
            {
                throw new ArgumentException("Số điện thoại không đúng định dạng Việt Nam (BR-SUPP-02).");
            }
        }

        // BR-SUPP-02: Email validation
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailTrim = email.Trim();
            if (!Regex.IsMatch(emailTrim, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                throw new ArgumentException("Địa chỉ Email không đúng định dạng (BR-SUPP-02).");
            }
        }
    }

    private static void ValidateLinkRequest(decimal purchasePrice, int leadTime, int moq, decimal rating)
    {
        // BR-SUPP-04
        if (purchasePrice < 0)
        {
            throw new ArgumentException("Giá nhập không được nhỏ hơn 0 (BR-SUPP-04).");
        }

        if (leadTime < 0)
        {
            throw new ArgumentException("Thời gian giao hàng (LeadTime) không được nhỏ hơn 0 (BR-SUPP-04).");
        }

        if (moq < 1)
        {
            throw new ArgumentException("Số lượng đặt hàng tối thiểu (MOQ) phải từ 1 trở lên (BR-SUPP-04).");
        }

        if (rating < 1.00m || rating > 5.00m)
        {
            throw new ArgumentException("Điểm đánh giá (Rating) phải từ 1.00 đến 5.00 điểm (BR-SUPP-04).");
        }
    }

    private static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto
        {
            SupplierId = supplier.SupplierId,
            SupplierCode = supplier.SupplierCode,
            SupplierName = supplier.SupplierName,
            ContactPerson = supplier.ContactPerson,
            PhoneNumber = supplier.PhoneNumber,
            Email = supplier.Email,
            Address = supplier.Address,
            TaxCode = supplier.TaxCode,
            LogoUrl = supplier.LogoUrl,
            Status = supplier.Status,
            ProductCount = supplier.ProductSuppliers?.Count ?? supplier.Products?.Count ?? 0,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        };
    }

    private static ProductSupplierDto MapToProductSupplierDto(ProductSupplier link)
    {
        return new ProductSupplierDto
        {
            ProductId = link.ProductId,
            ProductName = link.Product?.ProductName ?? string.Empty,
            Barcode = link.Product?.Barcode ?? string.Empty,
            SupplierId = link.SupplierId,
            SupplierName = link.Supplier?.SupplierName ?? string.Empty,
            PurchasePrice = link.PurchasePrice,
            SupplierProductCode = link.SupplierProductCode,
            LeadTime = link.LeadTime,
            MinimumOrderQuantity = link.MinimumOrderQuantity,
            Rating = link.Rating,
            IsDefault = link.IsDefault,
            CreatedAt = link.CreatedAt
        };
    }
}
