using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using SmartSupermarket.Backend.Domain.Entities;
using SmartSupermarket.Backend.Domain.Enums;
using SmartSupermarket.Backend.Features.Auth.Repositories;
using SmartSupermarket.Backend.Features.Customers.DTOs;
using SmartSupermarket.Backend.Features.Customers.Repositories;
using SmartSupermarket.Backend.Infrastructure.Security;

namespace SmartSupermarket.Backend.Features.Customers.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;

    private static readonly Dictionary<int, (int MinPoints, decimal VoucherValue, string Label)> _tiers = new()
    {
        { 500,   (500,   20000m,  "Voucher 20.000đ") },
        { 1000,  (1000,  50000m,  "Voucher 50.000đ") },
        { 2000,  (2000,  120000m, "Voucher 120.000đ") },
        { 5000,  (5000,  350000m, "Voucher 350.000đ") },
    };

    public CustomerService(
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        PasswordHasher passwordHasher)
    {
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<CustomerResponseDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCustomerInput(request.FullName, request.Phone, request.Email);

        string cleanPhone = request.Phone.Trim();
        var existingActive = await _customerRepository.GetActiveByPhoneAsync(cleanPhone, null, cancellationToken);
        if (existingActive != null)
        {
            throw new InvalidOperationException($"Số điện thoại {cleanPhone} đã được đăng ký cho một khách hàng đang hoạt động.");
        }

        // 1. Create or link User
        string email = string.IsNullOrWhiteSpace(request.Email)
            ? $"{cleanPhone}@customer.smartmarket.vn"
            : request.Email.Trim();

        var existingUser = await _userRepository.GetByPhoneAsync(cleanPhone);
        User user;

        if (existingUser != null)
        {
            user = existingUser;
            user.FullName = request.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                user.Email = email;
            }
            if (request.DateOfBirth.HasValue)
            {
                user.DateOfBirth = request.DateOfBirth.Value.ToUniversalTime();
            }
            user.Status = UserStatus.Active;
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.UpdateUser(user);
        }
        else
        {
            user = new User
            {
                Username = cleanPhone,
                PasswordHash = _passwordHasher.HashPassword($"Customer@{cleanPhone[^Math.Min(4, cleanPhone.Length)..]}"),
                FullName = request.FullName.Trim(),
                Email = email,
                PhoneNumber = cleanPhone,
                DateOfBirth = request.DateOfBirth?.ToUniversalTime(),
                Role = UserRole.Customer,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.AddUserAsync(user);
        }

        await _userRepository.SaveChangesAsync();

        // 2. Create Customer Profile
        var customer = new Customer
        {
            UserId = user.UserId,
            User = user,
            LoyaltyPoints = 0,
            MembershipTier = 1,
            Gender = request.Gender,
            Address = request.Address?.Trim(),
            Status = 1, // 1 = ACTIVE
            CreatedAt = DateTime.UtcNow
        };

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _customerRepository.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(customer);
    }

    public async Task<CustomerResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        return customer == null ? null : MapToResponseDto(customer);
    }

    public async Task<CustomerLookupDto?> LookupByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var customer = await _customerRepository.GetByPhoneAsync(phone.Trim(), cancellationToken);
        return customer == null ? null : MapToLookupDto(customer);
    }

    public async Task<CustomerPagedResponse<CustomerListItemDto>> GetPagedAsync(
        string? keyword, byte? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        int p = page <= 0 ? 1 : page;
        int ps = pageSize <= 0 ? 10 : pageSize;

        var (items, totalCount) = await _customerRepository.GetPagedAsync(keyword, status, p, ps, cancellationToken);

        return new CustomerPagedResponse<CustomerListItemDto>
        {
            Page = p,
            PageSize = ps,
            TotalCount = totalCount,
            Items = items.Select(MapToListItemDto).ToList()
        };
    }

    public async Task<CustomerResponseDto> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khách hàng có ID = {id}");
        }

        ValidateCustomerInput(request.FullName, request.Phone, request.Email);

        string cleanPhone = request.Phone.Trim();
        var duplicate = await _customerRepository.GetActiveByPhoneAsync(cleanPhone, id, cancellationToken);
        if (duplicate != null)
        {
            throw new InvalidOperationException($"Số điện thoại {cleanPhone} đã được đăng ký bởi khách hàng khác.");
        }

        // Update User info
        if (customer.User != null)
        {
            customer.User.FullName = request.FullName.Trim();
            customer.User.PhoneNumber = cleanPhone;
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                customer.User.Email = request.Email.Trim();
            }
            if (request.DateOfBirth.HasValue)
            {
                customer.User.DateOfBirth = request.DateOfBirth.Value.ToUniversalTime();
            }
            customer.User.UpdatedAt = DateTime.UtcNow;
        }

        // Update Customer info (CustomerId, LoyaltyPoints, CreatedAt are immutable here)
        customer.Gender = request.Gender;
        customer.Address = request.Address?.Trim();
        if (request.Status.HasValue)
        {
            customer.Status = request.Status.Value;
        }
        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);
        await _customerRepository.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(customer);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khách hàng có ID = {id}");
        }

        // Soft delete per Rule 10: Status = 2 (INACTIVE)
        customer.Status = 2;
        customer.UpdatedAt = DateTime.UtcNow;
        if (customer.User != null)
        {
            customer.User.Status = UserStatus.Locked;
            customer.User.UpdatedAt = DateTime.UtcNow;
        }

        _customerRepository.Update(customer);
        await _customerRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CustomerLoyaltyDto> GetLoyaltyPointsAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khách hàng có ID = {id}");
        }

        return new CustomerLoyaltyDto
        {
            CustomerId = customer.CustomerId,
            LoyaltyPoints = customer.LoyaltyPoints
        };
    }

    public async Task<PointHistoryListResponse> GetLoyaltyHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khách hàng có ID = {id}");
        }

        var histories = await _customerRepository.GetPointHistoryAsync(id, cancellationToken);

        return new PointHistoryListResponse
        {
            Items = histories.Select(h => new PointHistoryItemDto
            {
                PointHistoryId = h.PointHistoryId,
                OrderId = h.OrderId,
                PointChange = h.PointChange,
                Type = h.Type,
                CreatedAt = h.CreatedAt
            }).ToList()
        };
    }

    public async Task<CustomerPagedResponse<CustomerOrderHistoryItemDto>> GetOrderHistoryAsync(
        int id, int? branchId, byte? status, DateTime? startDate, DateTime? endDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khách hàng có ID = {id}");
        }

        int p = page <= 0 ? 1 : page;
        int ps = pageSize <= 0 ? 10 : pageSize;

        var (orders, totalCount) = await _customerRepository.GetOrdersAsync(
            id, branchId, status, startDate, endDate, p, ps, cancellationToken);

        return new CustomerPagedResponse<CustomerOrderHistoryItemDto>
        {
            Page = p,
            PageSize = ps,
            TotalCount = totalCount,
            Items = orders.Select(o => new CustomerOrderHistoryItemDto
            {
                OrderId = o.OrderId,
                BranchId = o.BranchId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                DiscountAmount = o.DiscountAmount,
                FinalAmount = o.FinalAmount,
                Status = (byte)o.Status
            }).ToList()
        };
    }

    public async Task<List<CustomerVoucherDto>> GetVouchersAsync(int id, string? status, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy khách hàng có ID = {id}");
        }

        var vouchers = await _customerRepository.GetVouchersAsync(id, status, cancellationToken);

        return vouchers.Select(v => new CustomerVoucherDto
        {
            VoucherId = v.VoucherId,
            Code = v.Code,
            IsUsed = v.IsUsed,
            ExpiryDate = v.ExpiryDate
        }).ToList();
    }

    // ==========================================
    // Backward compatibility methods for Desktop POS
    // ==========================================
    public async Task<CustomerDto?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByPhoneAsync(phone, cancellationToken);
        return customer == null ? null : MapToLegacyDto(customer);
    }

    public async Task<CustomerDto> AddPointsAsync(int customerId, AddPointsRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng có ID = {customerId}");

        if (request.PointsToAdd <= 0)
            throw new ArgumentException("Số điểm cộng phải lớn hơn 0.");

        customer.LoyaltyPoints += request.PointsToAdd;
        customer.MembershipTier = CalculateTierLevel(customer.LoyaltyPoints);

        // Record PointHistory
        await _customerRepository.AddPointHistoryAsync(new PointHistory
        {
            CustomerId = customer.CustomerId,
            PointChange = request.PointsToAdd,
            Type = 1,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        _customerRepository.Update(customer);
        await _customerRepository.SaveChangesAsync(cancellationToken);

        return MapToLegacyDto(customer);
    }

    public async Task<RedeemVoucherResponse> RedeemVoucherAsync(int customerId, RedeemVoucherRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng có ID = {customerId}");

        var tierEntry = _tiers
            .Where(t => request.PointsToRedeem >= t.Key)
            .OrderByDescending(t => t.Key)
            .Select(t => t.Value)
            .FirstOrDefault();

        if (tierEntry == default)
        {
            return new RedeemVoucherResponse
            {
                IsSuccess = false,
                Message = $"Cần ít nhất 500 điểm để đổi voucher. Bạn hiện có {customer.LoyaltyPoints} điểm.",
                RemainingPoints = customer.LoyaltyPoints
            };
        }

        if (customer.LoyaltyPoints < tierEntry.MinPoints)
        {
            return new RedeemVoucherResponse
            {
                IsSuccess = false,
                Message = $"Không đủ điểm. Cần {tierEntry.MinPoints} điểm, bạn có {customer.LoyaltyPoints} điểm.",
                RemainingPoints = customer.LoyaltyPoints
            };
        }

        customer.LoyaltyPoints -= tierEntry.MinPoints;
        customer.MembershipTier = CalculateTierLevel(customer.LoyaltyPoints);

        string voucherCode = $"LOYALTY-{customer.CustomerId:D4}-{DateTime.UtcNow:MMddHHmmss}";

        // Record PointHistory (Type 3 = Adjustment / Redemption)
        await _customerRepository.AddPointHistoryAsync(new PointHistory
        {
            CustomerId = customer.CustomerId,
            PointChange = -tierEntry.MinPoints,
            Type = 3,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        // Persist personal voucher to Vouchers table
        await _customerRepository.AddVoucherAsync(new Voucher
        {
            CustomerId = customer.CustomerId,
            Code = voucherCode,
            DiscountAmount = tierEntry.VoucherValue,
            MinimumOrderAmount = 0,
            ExpiryDate = DateTime.UtcNow.AddMonths(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        _customerRepository.Update(customer);
        await _customerRepository.SaveChangesAsync(cancellationToken);

        return new RedeemVoucherResponse
        {
            IsSuccess = true,
            Message = $"Đổi điểm thành công! {tierEntry.Label} đã được tạo.",
            VoucherCode = voucherCode,
            VoucherValue = tierEntry.VoucherValue,
            RemainingPoints = customer.LoyaltyPoints
        };
    }

    private static void ValidateCustomerInput(string fullName, string phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Họ và tên khách hàng không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("Số điện thoại không được để trống.");
        }

        string cleanPhone = phone.Trim();
        if (!Regex.IsMatch(cleanPhone, @"^(0|\+84)[0-9]{9,10}$"))
        {
            throw new ArgumentException("Số điện thoại không đúng định dạng.");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            string cleanEmail = email.Trim();
            if (!new EmailAddressAttribute().IsValid(cleanEmail))
            {
                throw new ArgumentException("Địa chỉ email không đúng định dạng.");
            }
        }
    }

    private static int CalculateTierLevel(int points)
    {
        if (points >= 5000) return 3;
        if (points >= 2000) return 2;
        if (points >= 500)  return 1;
        return 0;
    }

    private static CustomerResponseDto MapToResponseDto(Customer c)
    {
        return new CustomerResponseDto
        {
            CustomerId = c.CustomerId,
            FullName = c.User?.FullName ?? string.Empty,
            Phone = c.User?.PhoneNumber ?? string.Empty,
            Email = c.User?.Email,
            DateOfBirth = c.User?.DateOfBirth,
            Gender = c.Gender,
            Address = c.Address,
            LoyaltyPoints = c.LoyaltyPoints,
            Status = c.Status
        };
    }

    private static CustomerListItemDto MapToListItemDto(Customer c)
    {
        return new CustomerListItemDto
        {
            CustomerId = c.CustomerId,
            FullName = c.User?.FullName ?? string.Empty,
            Phone = c.User?.PhoneNumber ?? string.Empty,
            Email = c.User?.Email,
            LoyaltyPoints = c.LoyaltyPoints,
            Status = c.Status
        };
    }

    private static CustomerLookupDto MapToLookupDto(Customer c)
    {
        return new CustomerLookupDto
        {
            CustomerId = c.CustomerId,
            FullName = c.User?.FullName ?? string.Empty,
            Phone = c.User?.PhoneNumber ?? string.Empty,
            LoyaltyPoints = c.LoyaltyPoints,
            Status = c.Status
        };
    }

    private static CustomerDto MapToLegacyDto(Customer c)
    {
        string tierLabel = c.MembershipTier switch
        {
            3 => "💎 Platinum VIP",
            2 => "👑 VIP Gold",
            1 => "🥈 Silver",
            _ => "🥉 Bronze"
        };

        return new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = $"KH-{c.CustomerId:D4}",
            UserId = c.UserId,
            FullName = c.User?.FullName ?? string.Empty,
            PhoneNumber = c.User?.PhoneNumber,
            Email = c.User?.Email,
            LoyaltyPoints = c.LoyaltyPoints,
            MembershipTier = tierLabel,
            MembershipTierLevel = c.MembershipTier,
            Status = c.Status
        };
    }
}
