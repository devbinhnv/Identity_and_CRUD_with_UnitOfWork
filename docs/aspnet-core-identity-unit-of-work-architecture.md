# ASP.NET Core Identity + Unit of Work

## Thiết kế Identity thống nhất với CRUD, Service và một Commit Boundary

> **Mục tiêu kiến trúc:** mọi thành phần nghiệp vụ chỉ thay đổi state
> trong cùng một `DbContext`; không `SaveChanges()` rải rác. Identity
> cũng tuân thủ quy ước này. Một Unit of Work duy nhất quyết định khi
> nào toàn bộ thay đổi được persist.

------------------------------------------------------------------------

## 1. Tư tưởng cốt lõi

Thiết kế này đặt ra các quy tắc:

1.  **Một use case = một Unit of Work = một commit boundary.**
2.  Repository **không gọi** `SaveChanges/SaveChangesAsync`.
3.  Application/Domain service **không tự commit**.
4.  Identity vẫn dùng `UserManager`, `RoleManager`, `SignInManager` để
    giữ nguyên validation và invariant của framework.
5.  EF Store của Identity được custom để **không auto-save**.
6.  Identity và business repositories dùng **cùng scoped
    `AppDbContext`** khi cần atomic commit.
7.  `Guid` được sinh ở application trước khi persist, nên các entity có
    thể liên kết bằng ID trước `SaveChanges`.
8.  Business module có thể giữ `UserId` như **logical reference**, không
    bắt buộc tạo FK/navigation tới Identity.
9.  Chỉ UoW/application pipeline được quyền quyết định persist/rollback.
10. Middleware có thể là outer boundary, nhưng commit phải xảy ra
    **trước khi response được gửi**. Với hệ thống lớn, application
    pipeline/command behavior thường là boundary an toàn hơn.

Mental model:

``` text
HTTP / Worker / Consumer
          |
          v
   Application Use Case
          |
   +------+-------+----------------+
   |              |                |
Identity Service  Repository A     Repository B
   |              |                |
UserManager       DbSet<T>         DbSet<T>
   |              |                |
Custom Store -----+----------------+
                  |
             AppDbContext
             ChangeTracker
                  |
           NO SAVE YET
                  |
                  v
             UnitOfWork
                  |
          SaveChangesAsync()
                  |
                  v
              Database
```

------------------------------------------------------------------------

## 2. Identity làm gì và project custom gì?

### Framework giữ trách nhiệm

ASP.NET Core Identity vẫn xử lý:

-   user/role management;
-   password hashing và password validation;
-   username/email normalization;
-   security stamp;
-   lockout;
-   claims;
-   role membership;
-   token provider;
-   2FA và account lifecycle khi sử dụng;
-   abstraction `UserManager`, `RoleManager`, `SignInManager`;
-   `IUserStore`/`IRoleStore`.

### Project chịu trách nhiệm

Project bổ sung:

-   `Guid` làm key;
-   schema/table mapping;
-   permission model riêng nếu cần;
-   Identity-facing application services;
-   custom EF stores với `AutoSaveChanges = false`;
-   Unit of Work;
-   transaction/commit boundary;
-   logical references từ business data tới `UserId`;
-   audit/domain-event interceptors;
-   error mapping và application result.

Điểm quan trọng: **không viết lại Identity**. Ta chỉ thay đổi
persistence behavior để Identity tuân theo UoW convention.

------------------------------------------------------------------------

## 3. Cấu trúc project đề xuất

``` text
src/
|
+-- Domain/
|   +-- Leads/
|   +-- Orders/
|   +-- Employees/
|
+-- Application/
|   +-- Abstractions/
|   |   +-- Identity/
|   |   |   +-- IUserService.cs
|   |   |   +-- IRoleService.cs
|   |   |   +-- IPermissionService.cs
|   |   +-- Persistence/
|   |       +-- IUnitOfWork.cs
|   |       +-- ILeadRepository.cs
|   |
|   +-- Users/
|   +-- Leads/
|   +-- Orders/
|
+-- Infrastructure/
|   +-- Identity/
|   |   +-- ApplicationUser.cs
|   |   +-- ApplicationRole.cs
|   |   +-- ApplicationUserStore.cs
|   |   +-- ApplicationRoleStore.cs
|   |   +-- UserService.cs
|   |   +-- RoleService.cs
|   |   +-- PermissionService.cs
|   |
|   +-- Persistence/
|       +-- AppDbContext.cs
|       +-- UnitOfWork.cs
|       +-- Repositories/
|       +-- Interceptors/
|
+-- Api/
    +-- Middleware/
    +-- Controllers/
```

Không cần ép Identity có `UserRepository` chỉ để giống các aggregate
khác. `UserManager` đã đứng trên store abstraction. Application nên wrap
Identity bằng **service theo capability/use case**.

------------------------------------------------------------------------

## 4. Identity entities dùng Guid

``` csharp
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
}

public sealed class ApplicationRole : IdentityRole<Guid>
{
}
```

`Guid` cho phép tạo ID trước khi persist:

``` csharp
var userId = Guid.NewGuid();

var user = new ApplicationUser
{
    Id = userId,
    UserName = request.Email,
    Email = request.Email
};

var employee = new Employee(
    id: Guid.NewGuid(),
    userId: userId);
```

Không cần save user trước chỉ để lấy database-generated identity key.

------------------------------------------------------------------------

## 5. Một AppDbContext cho Identity + business data

``` csharp
public sealed class AppDbContext
    : IdentityDbContext<
        ApplicationUser,
        ApplicationRole,
        Guid>
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Order> Orders => Set<Order>();

    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureIdentity(builder);
        ConfigureBusiness(builder);
    }

    private static void ConfigureIdentity(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>()
            .ToTable("Users", "identity");

        builder.Entity<ApplicationRole>()
            .ToTable("Roles", "identity");

        builder.Entity<IdentityUserRole<Guid>>()
            .ToTable("UserRoles", "identity");

        builder.Entity<IdentityUserClaim<Guid>>()
            .ToTable("UserClaims", "identity");

        builder.Entity<IdentityRoleClaim<Guid>>()
            .ToTable("RoleClaims", "identity");

        builder.Entity<IdentityUserLogin<Guid>>()
            .ToTable("UserLogins", "identity");

        builder.Entity<IdentityUserToken<Guid>>()
            .ToTable("UserTokens", "identity");
    }

    private static void ConfigureBusiness(ModelBuilder builder)
    {
        builder.Entity<Employee>()
            .ToTable("Employees", "hr");

        builder.Entity<Lead>()
            .ToTable("Leads", "crm");

        builder.Entity<Order>()
            .ToTable("Orders", "sales");
    }
}
```

Một `DbContext` không đồng nghĩa các module bị trộn logic. Boundary vẫn
được giữ bằng application interface/service/repository/schema.

------------------------------------------------------------------------

## 6. Không tạo FK từ business module tới Identity khi muốn loose coupling

Ví dụ:

``` csharp
public sealed class Employee
{
    public Guid Id { get; private set; }

    // Logical reference tới Identity User.
    public Guid UserId { get; private set; }

    private Employee() { }

    public Employee(Guid id, Guid userId)
    {
        Id = id;
        UserId = userId;
    }
}
```

Không khai báo:

``` csharp
public ApplicationUser User { get; set; }
```

và không cấu hình:

``` text
hr.Employees.UserId -> identity.Users.Id
```

### Lợi ích

-   giảm coupling giữa business domain và Identity persistence model;
-   không có cascade/delete constraint ngoài ý muốn;
-   dễ tách Identity sang database/service khác về sau;
-   historical record vẫn giữ `UserId`.

### Đổi lại

Database không enforce referential integrity. Application phải validate
khi use case yêu cầu user tồn tại.

Không FK **không đồng nghĩa nên hard-delete user**. Với account đã xuất
hiện trong audit/history, thường nên disable/soft-delete:

``` csharp
user.IsActive = false;
```

------------------------------------------------------------------------

## 7. Custom UserStore để Identity không SaveChanges

Đây là phần then chốt.

``` csharp
public sealed class ApplicationUserStore
    : UserStore<
        ApplicationUser,
        ApplicationRole,
        AppDbContext,
        Guid>
{
    public ApplicationUserStore(
        AppDbContext context,
        IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        AutoSaveChanges = false;
    }
}
```

Role store:

``` csharp
public sealed class ApplicationRoleStore
    : RoleStore<
        ApplicationRole,
        AppDbContext,
        Guid>
{
    public ApplicationRoleStore(
        AppDbContext context,
        IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        AutoSaveChanges = false;
    }
}
```

Khi đó:

``` csharp
await _userManager.CreateAsync(user, password);
```

vẫn đi qua:

``` text
UserManager
   |
   +-- validation
   +-- normalization
   +-- password hashing
   +-- security logic
   |
   v
ApplicationUserStore
   |
   v
DbContext ChangeTracker
   |
   X  không SaveChanges ở đây
```

### Semantics cần hiểu

Sau khi tắt auto-save:

``` csharp
IdentityResult result =
    await _userManager.CreateAsync(user, password);
```

`result.Succeeded == true` nghĩa là operation của Identity/store đã
thành công ở tầng hiện tại.

Nó **không còn đảm bảo transaction đã commit xuống database**.

Persist thực sự chỉ thành công sau:

``` csharp
await _unitOfWork.CommitAsync();
```

Database constraint/concurrency/connection vẫn có thể làm commit fail.

------------------------------------------------------------------------

## 8. Đăng ký custom stores

Cấu hình cụ thể phụ thuộc cách project đăng ký Identity, nhưng nguyên
tắc là `UserManager` và `RoleManager` phải resolve custom store thay vì
EF store auto-save mặc định.

Ví dụ:

``` csharp
services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Identity options...
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

services.AddScoped<IUserStore<ApplicationUser>, ApplicationUserStore>();
services.AddScoped<IRoleStore<ApplicationRole>, ApplicationRoleStore>();
```

Cần kiểm tra registration cuối cùng của project để đảm bảo custom
registration là implementation thực tế được resolve. Không nên giữ hai
registration mơ hồ mà không verify DI behavior.

------------------------------------------------------------------------

## 9. Service layer cho Identity

Không tạo repository CRUD giả quanh `UserManager`.

Không nên:

``` text
UserService
   |
UserRepository
   |
UserManager
   |
IUserStore
```

Nên:

``` text
Application
   |
IUserService
   |
Infrastructure.UserService
   |
UserManager
   |
Custom UserStore
```

Interface:

``` csharp
public interface IUserService
{
    Task<Result<Guid>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> AssignRoleAsync(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
```

Implementation:

``` csharp
internal sealed class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!result.Succeeded)
        {
            return Result.Failure<Guid>(
                MapErrors(result.Errors));
        }

        // Không SaveChanges.
        return Result.Success(user.Id);
    }

    public async Task<bool> ExistsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _userManager.Users
            .AnyAsync(x => x.Id == userId, cancellationToken);
    }
}
```

Service trả application result, không leak `IdentityResult` ra toàn
application nếu muốn giảm dependency vào framework.

------------------------------------------------------------------------

## 10. Repository convention

Repository chỉ thao tác aggregate/data state.

``` csharp
public interface IEmployeeRepository
{
    void Add(Employee employee);

    Task<Employee?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
```

Implementation:

``` csharp
internal sealed class EmployeeRepository
    : IEmployeeRepository
{
    private readonly AppDbContext _db;

    public EmployeeRepository(AppDbContext db)
    {
        _db = db;
    }

    public void Add(Employee employee)
    {
        _db.Employees.Add(employee);
    }

    public Task<Employee?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _db.Employees
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }
}
```

**Không có:**

``` csharp
await _db.SaveChangesAsync();
```

Repository không sở hữu transaction boundary.

------------------------------------------------------------------------

## 11. Unit of Work

Interface:

``` csharp
public interface IUnitOfWork
{
    Task<int> CommitAsync(
        CancellationToken cancellationToken = default);
}
```

Implementation:

``` csharp
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    public Task<int> CommitAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
```

DI:

``` csharp
services.AddScoped<IUnitOfWork, UnitOfWork>();
```

Vì `AppDbContext`, Identity stores, services, repositories và UoW đều
scoped, chúng có thể cùng làm việc trên một request-scoped context.

------------------------------------------------------------------------

## 12. Một use case hoàn chỉnh

Ví dụ tạo account + employee:

``` csharp
public sealed class CreateEmployeeHandler
{
    private readonly IUserService _users;
    private readonly IEmployeeRepository _employees;

    public CreateEmployeeHandler(
        IUserService users,
        IEmployeeRepository employees)
    {
        _users = users;
        _employees = employees;
    }

    public async Task<Result<Guid>> Handle(
        CreateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var userResult = await _users.CreateAsync(
            new CreateUserRequest(
                command.Email,
                command.Password),
            cancellationToken);

        if (userResult.IsFailure)
            return Result.Failure<Guid>(userResult.Error);

        var employee = new Employee(
            Guid.NewGuid(),
            userResult.Value);

        _employees.Add(employee);

        // Không commit.
        return Result.Success(employee.Id);
    }
}
```

Trước commit:

``` text
ChangeTracker
|
+-- ApplicationUser      Added
+-- Employee             Added
+-- IdentityUserRole     Added (nếu assign role)
+-- ...                  Modified
```

Sau đó outer UoW commit toàn bộ.

------------------------------------------------------------------------

## 13. Commit ở Middleware: có thể, nhưng phải hiểu boundary

Concept:

``` csharp
public sealed class UnitOfWorkMiddleware
{
    private readonly RequestDelegate _next;

    public UnitOfWorkMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IUnitOfWork unitOfWork)
    {
        await _next(context);

        // Điều kiện commit phải được thiết kế rõ.
        await unitOfWork.CommitAsync(context.RequestAborted);
    }
}
```

Flow:

``` text
Middleware enters
      |
      v
Controller
      |
Application use case
      |
Identity + Repositories
      |
ChangeTracker
      |
Controller completes
      |
Middleware regains control
      |
Commit
      |
Response
```

### Rủi ro quan trọng

Không được để response đã bắt đầu gửi rồi mới phát hiện commit fail.

``` text
Response started: 200 OK
        |
        v
SaveChanges()
        |
        X DB failure
```

Client có thể nhận success trong khi database không commit.

Vì vậy nếu dùng middleware:

-   không commit sau streaming/file response;
-   kiểm soát `Response.HasStarted`;
-   exception phải ngăn commit;
-   xác định rõ request nào là transactional;
-   đảm bảo response buffering/boundary phù hợp nếu commit thực sự nằm
    sau `_next`;
-   không dựa duy nhất vào `StatusCode < 400` như business success
    signal.

------------------------------------------------------------------------

## 14. Boundary khuyến nghị: Application/UoW pipeline

Mental model tổng quát tốt hơn là:

> **Một use case = một commit**, không phải tuyệt đối "một HTTP request
> = một commit".

Ví dụ pipeline behavior:

``` text
HTTP Controller
      |
      v
Transaction/UoW Behavior
      |
      v
Command Handler
      |
      +-- Identity
      +-- Repository
      +-- Domain
      |
      v
Commit
      |
      v
Return Result
      |
      v
HTTP Response
```

Pseudo-code:

``` csharp
public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    var response = await next();

    await _unitOfWork.CommitAsync(cancellationToken);

    return response;
}
```

Ưu điểm:

-   commit trước khi controller tạo/gửi final response;
-   áp dụng được cho background worker/message consumer;
-   GET/query không cần UoW write commit;
-   transaction boundary bám vào use case thay vì transport protocol.

Nếu project cũ đã chuẩn hóa middleware UoW và xử lý response boundary
tốt, vẫn có thể giữ middleware. Kiến trúc cốt lõi không thay đổi.

------------------------------------------------------------------------

## 15. Có cần explicit database transaction không?

Một lần `SaveChangesAsync()` của EF Core sẽ có transactional behavior
cho batch thay đổi phù hợp. Tuy nhiên explicit transaction cần cân nhắc
khi use case có nhiều lần flush, raw SQL, nhiều context hoặc external
operation.

Trong kiến trúc này, mục tiêu mặc định là:

``` text
Mutate
Mutate
Mutate
Mutate
   |
ONE SaveChanges
```

không phải:

``` text
BeginTransaction
Save
Save
Save
Commit
```

Nếu có use case thật sự cần nhiều `SaveChanges` trong cùng transaction,
UoW có thể được mở rộng:

``` csharp
public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken ct = default);
    Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);
}
```

Nhưng không thêm complexity này nếu chưa có nhu cầu.

------------------------------------------------------------------------

## 16. SaveChangesInterceptor dùng cho việc gì?

Interceptor **không nên** được dùng để làm `SaveChanges()` thành no-op.

Không nên:

``` text
SaveChanges
   |
Interceptor
   |
"Không save"
```

Vì method semantics bị phá vỡ.

Interceptor hợp với cross-cutting persistence concerns:

``` text
SaveChanges
   |
   +-- audit timestamps
   +-- soft-delete conversion
   +-- domain-event collection
   +-- outbox messages
   +-- tenant metadata
   +-- logging
```

Ví dụ:

``` csharp
public sealed class AuditableEntityInterceptor
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>>
        SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
    {
        // set CreatedAt / UpdatedAt...
        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }
}
```

**Identity auto-save được tắt tại Store; UoW quyết định commit;
interceptor enrich quá trình commit.**

------------------------------------------------------------------------

## 17. Permission service

Identity không có permission model chuẩn. Có thể thêm:

``` text
authorization.Permissions
authorization.RolePermissions
```

Ví dụ:

``` csharp
public sealed class Permission
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
}

public sealed class RolePermission
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
}
```

`RoleId` có thể là logical reference hoặc FK tùy boundary mong muốn.

Application abstraction:

``` csharp
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        string permission,
        CancellationToken cancellationToken = default);

    Task GrantToRoleAsync(
        Guid roleId,
        string permission,
        CancellationToken cancellationToken = default);
}
```

`GrantToRoleAsync` cũng chỉ mutate state; UoW commit sau.

Authorization runtime có thể dùng policy/requirement/handler và cache
permission phù hợp, thay vì query permission một cách tùy tiện ở mọi
controller.

------------------------------------------------------------------------

## 18. Quy tắc về SaveChanges trong toàn project

Nên enforce bằng convention/code review/analyzer nếu cần:

### Được gọi `SaveChangesAsync`

``` text
UnitOfWork
Persistence pipeline
Migration/seeding infrastructure (ngoại lệ có chủ đích)
```

### Không được gọi

``` text
Controller
Application Service
Domain Service
Repository
Identity Service
UserStore business wrapper
Role Service
Permission Service
```

Search toàn solution cho:

``` text
SaveChanges(
SaveChangesAsync(
```

nên cho ra rất ít điểm có chủ đích.

------------------------------------------------------------------------

## 19. Error và rollback

Flow chuẩn:

``` text
Use Case
 |
 +-- Create User       tracked
 +-- Assign Role       tracked
 +-- Create Employee   tracked
 +-- Create Audit      tracked
 |
 v
Commit
 |
 +-- success -> toàn bộ persist
 |
 X-- exception -> request/use case failure
```

Nếu chưa `SaveChanges`, exception giữa use case đơn giản khiến scoped
context bị bỏ đi; không có partial persistence từ các operation trước.

Nếu commit ném `DbUpdateException`/concurrency exception, outer
application boundary map lỗi sang result/HTTP response phù hợp.

Không nên catch exception trong repository rồi giả vờ operation thành
công.

------------------------------------------------------------------------

## 20. Side effects ngoài database

UoW chỉ atomic với resource mà transaction quản lý.

Ví dụ nguy hiểm:

``` text
Add User to ChangeTracker
Send Email       <-- đã gửi thật
Add Employee
SaveChanges      <-- fail
```

Email không rollback được.

Với side effect quan trọng nên dùng pattern như **Outbox**:

``` text
Use Case
 |
 +-- User Added
 +-- Employee Added
 +-- OutboxMessage Added
 |
 v
ONE SaveChanges
 |
 v
Background Publisher
 |
 v
Email / Message Broker
```

Như vậy quyết định "cần gửi email/event" được commit cùng business data.

------------------------------------------------------------------------

## 21. Các trường hợp không nên áp dụng UoW write commit máy móc

Không phải request nào cũng cần commit:

-   GET/query;
-   health check;
-   static/file/stream response;
-   authentication request không mutate application state;
-   long-running streaming;
-   WebSocket;
-   external callbacks cần transaction riêng;
-   background operations có lifetime khác request.

Đó là lý do "use case boundary" là abstraction tổng quát hơn "HTTP
middleware boundary".

------------------------------------------------------------------------

## 22. Testing strategy

### Unit test service

Mock `UserManager`/abstraction cần thiết hoặc test qua `IUserService`
behavior.

Xác minh:

-   service không commit;
-   Identity errors được map đúng;
-   ID được tạo trước persist;
-   application result đúng.

### Integration test UoW

Test quan trọng:

``` text
Create Identity User
+
Create Employee
+
Commit
```

và verify cả hai cùng tồn tại.

Sau đó cố tình gây lỗi business constraint ở commit và verify không có
partial state.

### Architecture test

Có thể viết test/rule đảm bảo:

-   repository không depend `IUnitOfWork`;
-   repository không gọi `SaveChanges`;
-   application không reference concrete `AppDbContext` nếu kiến trúc
    yêu cầu;
-   Domain không reference ASP.NET Core Identity;
-   business entities không navigation tới `ApplicationUser` nếu
    boundary yêu cầu loose coupling.

------------------------------------------------------------------------

## 23. Những anti-pattern cần tránh

### 23.1 Repository wrap UserManager một cách máy móc

``` text
UserRepository -> UserManager -> IUserStore
```

Nếu repository không thêm semantic/business boundary thì chỉ tăng
abstraction.

### 23.2 Mỗi service tự SaveChanges

Phá vỡ commit boundary:

``` text
Service A -> Save
Service B -> fail
```

### 23.3 Dùng interceptor để nuốt SaveChanges

Caller tưởng đã persist nhưng thực tế chưa persist.

### 23.4 Identity bypass UserManager

Không nên:

``` csharp
_db.Users.Add(user);
```

cho account creation chỉ để đồng bộ với CRUD. Việc này có thể bypass
password/validation/normalization/security behavior.

### 23.5 Hard-delete user vì không có FK

Không FK giải quyết coupling/constraint, không tự động giải quyết
historical identity/audit.

### 23.6 Commit sau khi response đã gửi

Có thể tạo trạng thái HTTP success nhưng DB failure.

------------------------------------------------------------------------

## 24. Flow cuối cùng đề xuất

``` text
                     APPLICATION BOUNDARY
+----------------------------------------------------------+
|                                                          |
|  CreateEmployee Use Case                                 |
|                                                          |
|  1. IUserService.CreateAsync()                           |
|       |                                                  |
|       v                                                  |
|     UserManager                                          |
|       |                                                  |
|     Custom UserStore                                     |
|       |                                                  |
|       +-----------> AppDbContext.ChangeTracker           |
|                                                          |
|  2. IRoleService.AssignAsync()                           |
|       +-----------> AppDbContext.ChangeTracker           |
|                                                          |
|  3. EmployeeRepository.Add()                             |
|       +-----------> AppDbContext.ChangeTracker           |
|                                                          |
|  4. Outbox.Add()                                         |
|       +-----------> AppDbContext.ChangeTracker           |
|                                                          |
|  5. UnitOfWork.CommitAsync()                             |
|                         |                                |
+-------------------------|--------------------------------+
                          v
                   SaveChangesAsync()
                          |
                    ONE DECISION
                          |
             +------------+------------+
             |                         |
           SUCCESS                    FAIL
             |                         |
       all persisted             no partial commit
```

------------------------------------------------------------------------

## 25. Checklist triển khai

-   [ ] `ApplicationUser : IdentityUser<Guid>`
-   [ ] `ApplicationRole : IdentityRole<Guid>`
-   [ ] Một scoped `AppDbContext` nếu cần atomic Identity + business
    commit
-   [ ] Identity tables map sang schema riêng
-   [ ] Business tables map theo module/schema
-   [ ] Không navigation/FK tới Identity nếu chủ đích là loose coupling
-   [ ] `ApplicationUserStore.AutoSaveChanges = false`
-   [ ] `ApplicationRoleStore.AutoSaveChanges = false`
-   [ ] Verify DI đang dùng custom stores
-   [ ] Wrap Identity bằng capability services, không CRUD repository
    giả
-   [ ] Repository không `SaveChanges`
-   [ ] Service không `SaveChanges`
-   [ ] `IUnitOfWork` là persistence decision point
-   [ ] Commit trước khi response được gửi
-   [ ] Prefer use-case/application pipeline boundary cho hệ thống tổng
    quát
-   [ ] Interceptor chỉ xử lý cross-cutting persistence concerns
-   [ ] External side effects dùng Outbox khi cần atomic intent
-   [ ] Integration test atomicity
-   [ ] Architecture test để chống `SaveChanges` bị gọi rải rác

------------------------------------------------------------------------

## 26. Nguyên tắc kiến trúc cuối cùng

> **Service và Repository mô tả thay đổi cần thực hiện. Unit of Work
> quyết định khi nào những thay đổi đó trở thành dữ liệu thật.**

Identity không phải ngoại lệ.

``` text
Identity Service
Business Service
Repository
Domain Logic
       |
       v
   Mutate State
       |
       v
   ChangeTracker
       |
       v
 UNIT OF WORK
       |
       v
    COMMIT
```

Đây là điểm tạo ra sự nhất quán: khi đọc bất kỳ service/repository nào,
developer biết chắc rằng operation đó **không âm thầm quyết định
persistence boundary**. Muốn biết dữ liệu thực sự được chấp nhận khi
nào, chỉ cần tìm UoW/application transaction boundary.
