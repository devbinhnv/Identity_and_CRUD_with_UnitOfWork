# Identity & CRUD with Unit of Work

Ví dụ tối giản: **ASP.NET Core Identity** và **CRUD** dùng chung một `DbContext` và **một Unit of Work**.
Không handler, repository hay service nào gọi `SaveChanges`. Identity cũng vậy. Chỉ Unit of Work quyết định khi nào thay đổi được lưu.

.NET 10 · EF Core 10 · PostgreSQL 17 · JWT · custom Mediator

Tài liệu thiết kế gốc: [`docs/aspnet-core-identity-unit-of-work-architecture.md`](docs/aspnet-core-identity-unit-of-work-architecture.md)

---

## 1. Chạy project

```bash
docker compose up -d                                            # PostgreSQL + pgAdmin
dotnet run --project src/IdentityUoW.Api --launch-profile http  # tự migrate + seed
```

| Thành phần | Địa chỉ | Tài khoản |
|---|---|---|
| API | http://localhost:5118 | admin seed: `admin@example.com` / `Admin@12345` |
| OpenAPI | http://localhost:5118/openapi/v1.json | – |
| pgAdmin | http://localhost:5050 | `admin@example.com` / `admin` |
| PostgreSQL | `localhost:5432`, DB `identity_uow` | `postgres` / `postgres` |

pgAdmin đã đăng ký sẵn server Postgres. Lần đầu kết nối chỉ cần nhập mật khẩu `postgres`.
Port và mật khẩu có thể đổi qua file `.env` (xem `.env.example`). `docker compose down -v` xoá toàn bộ dữ liệu.

Có thể chạy thử toàn bộ luồng bằng [`src/IdentityUoW.Api/IdentityUoW.Api.http`](src/IdentityUoW.Api/IdentityUoW.Api.http).

---

## 2. Cấu trúc

Tất cả nằm trong **một project** `src/IdentityUoW.Api`, chia folder theo trách nhiệm:

```text
src/IdentityUoW.Api/
├── Common/                         Building blocks dùng chung
│   ├── Domains/                    EntityBase<TKey>, EntityAuditBase<TKey>, IDateTracking
│   ├── Mediator/                   IRequest, ICommand, IRequestHandler, IMediator, Mediator  ← commit boundary
│   ├── Models/                     ApiResult / ApiSuccessResult / ApiErrorResult, PageList, MetaData, PagingRequestParameters
│   └── Repositories/               IUnitOfWork<TContext>, UnitOfWork<TContext>, RepositoryBaseAsync
├── Identity/                       UserStore, RoleStore (custom, no auto-save)
├── Persistence/
│   ├── AppDbContext.cs             Một DbContext cho Identity + business, tự ghi audit
│   ├── AppDbContextSeed.cs         Role Admin/User + tài khoản admin
│   ├── Schemas.cs                  identity, catalog
│   ├── Configurations/             IdentityConfiguration (đổi tên bảng), ProductConfiguration
│   └── Migrations/
├── Entities/                       UserEntity, RoleEntity (Identity) + ProductEntity (ví dụ CRUD)
├── Repositories/                   IProductRepository, ProductRepository
├── Services/                       UserService (bọc UserManager), TokenService (JWT)
├── Features/                       Mỗi use case một folder: Command/Query + Handler
│   ├── Auth/Commands/Register      user + role, commit một lần
│   ├── Auth/Queries/Login
│   ├── Products/Commands/          CreateProduct, UpdateProduct, DeleteProduct
│   └── Products/Queries/           GetProducts (phân trang + search), GetProductById
├── Dtos/
├── Controllers/                    Controller mỏng, chỉ gọi IMediator
├── Configurations/                 JwtSettings
├── Extensions/                     ServiceExtensions, ApplicationExtensions, HostExtensions (migrate + seed)
└── Program.cs
```

Luồng phụ thuộc: `Controller → IMediator → Handler → Service/Repository → AppDbContext (ChangeTracker)` rồi `Mediator → IUnitOfWork.CommitAsync()`.

---

## 3. Database: schema và tên bảng

| Schema | Bảng |
|---|---|
| `identity` | `users`, `roles`, `user_roles`, `user_claims`, `user_logins`, `user_tokens`, `role_claims` |
| `catalog` | `products` |
| `public` | `__ef_migrations_history` |

Tên bảng Identity được đổi trong `Persistence/Configurations/IdentityConfiguration.cs`:

| Mặc định | Sau khi custom |
|---|---|
| `AspNetUsers` | `identity.users` |
| `AspNetRoles` | `identity.roles` |
| `AspNetUserRoles` | `identity.user_roles` |
| `AspNetUserClaims` | `identity.user_claims` |
| `AspNetUserLogins` | `identity.user_logins` |
| `AspNetUserTokens` | `identity.user_tokens` |
| `AspNetRoleClaims` | `identity.role_claims` |
| `UserNameIndex` / `EmailIndex` / `RoleNameIndex` | `ix_users_normalized_user_name` / `ix_users_normalized_email` (unique) / `ix_roles_normalized_name` |

Quy ước đặt tên:

- Mọi bảng và cột dùng `snake_case`.
- Đặt tên constraint theo mẫu `pk_<table>`, `fk_<table>_<principal>_<column>`, `ix_<table>_<column>`.
- Khoá chính là UUIDv7 (`Guid.CreateVersion7()`), sinh ngay ở application. Nhờ đó Id có sẵn trước khi commit, và vì tăng dần theo thời gian nên index không bị phân mảnh.
- `catalog.products.created_by` chỉ là **logical reference** tới `identity.users.id`: không có FK, không có navigation. Phần nghiệp vụ không bị buộc vào model của Identity.

---

## 4. Pattern đang áp dụng

### 4.1 Unit of Work

```csharp
public sealed class UnitOfWork<TContext>(TContext context) : IUnitOfWork<TContext> where TContext : DbContext
{
    public Task<int> CommitAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
```

Đây là nơi **duy nhất** gọi `SaveChanges`. Bản thân một lần `SaveChanges` của EF Core đã chạy trong một transaction, nên mọi thay đổi của Identity và của nghiệp vụ được commit nguyên tử. Không cần gọi `BeginTransaction`.

### 4.2 Custom Mediator, đồng thời là commit boundary

`IRequest<T>` là query, `ICommand<T>` là command. Mediator resolve handler qua DI, sau đó:

| Request | Kết quả handler | Hành vi |
|---|---|---|
| `IRequest<T>` (query) | bất kỳ | trả kết quả, không commit |
| `ICommand<T>` | `ApiResult.IsSucceeded == true` | `CommitAsync()` **một lần** |
| `ICommand<T>` | thất bại hoặc ném exception | không commit; DbContext scoped bị bỏ khi request kết thúc |

Commit xảy ra **trước khi** controller trả response. Vì vậy client không bao giờ nhận `200 OK` cho dữ liệu chưa được lưu. Đây là lý do commit đặt trong mediator chứ không đặt trong middleware.

### 4.3 Repository chỉ thay đổi trạng thái

`RepositoryBaseAsync` cung cấp `FindAll`, `FindByCondition`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`.
So với generic repository thông thường, ở đây cố ý **bỏ** `SaveChangeAsync`, `BeginTransactionAsync`, `EndTransactionAsync`: repository không sở hữu transaction. `GetByIdAsync` dùng `FindAsync`, nên tìm thấy cả entity vừa thêm trong cùng Unit of Work.

### 4.4 Custom Identity Store: Identity cũng tuân theo UoW

```csharp
public class UserStore : UserStore<UserEntity, RoleEntity, AppDbContext, Guid>
{
    public UserStore(AppDbContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer) => AutoSaveChanges = false;
}
```

`UserManager` vẫn giữ toàn bộ validation, normalization, password hashing và security stamp, nhưng chỉ ghi vào ChangeTracker.
Store được đăng ký bằng `AddIdentityCore().AddUserStore<...>().AddRoleStore<...>()` và **không** gọi `AddEntityFrameworkStores`. Như vậy trong DI chỉ có đúng một store cho user và một cho role, không còn store mặc định nào tự lưu.

> `IdentityResult.Succeeded` giờ nghĩa là *đã được track*, chưa phải *đã commit*.

### 4.5 Service bọc Identity thay vì "UserRepository"

`IUserService` bọc `UserManager` theo từng năng lực (create, assign role, kiểm tra mật khẩu). Không tạo một repository CRUD giả quanh `UserManager`, và không bao giờ gọi `db.Users.Add(user)` (cách đó bỏ qua toàn bộ validation và hashing của Identity).

### 4.6 Các pattern khác

- **ApiResult**: mọi handler trả `ApiSuccessResult<T>` hoặc `ApiErrorResult<T>` (có `StatusCode`). Controller chỉ cần gọi `result.ToActionResult()`.
- **Audit tự động**: `AppDbContext.SaveChangesAsync` tự điền `CreatedDate`/`LastModifiedDate` cho mọi `IDateTracking`, kể cả `UserEntity`.
- **Phân trang**: `PagingRequestParameters` → `PageList<T>` + `MetaData`.
- **Extensions**: `AddInfrastructure()`, `UseInfrastructure()`, `MigrateDatabaseAsync<TContext>()` giữ `Program.cs` ở mức vài dòng.

---

## 5. Luồng ví dụ: Register

```text
POST /api/auth/register
  └─ AuthController ─► IMediator.SendAsync(RegisterCommand)
        └─ RegisterCommandHandler
              ├─ UserService.CreateAsync     → UserManager → UserStore → identity.users      [Added]
              └─ UserService.AssignRoleAsync → UserManager → UserStore → identity.user_roles [Added]
        handler trả ApiSuccessResult
  └─ Mediator: request là ICommand + thành công ─► UnitOfWork.CommitAsync()  (MỘT SaveChanges)
  └─ 200 OK
```

Nếu gán role thất bại, handler trả `ApiErrorResult` nên mediator không commit, và user cũng **không** được lưu.

---

## 6. Nguyên tắc thiết kế

1. **Một command = một Unit of Work = một commit.**
2. **Chỉ `UnitOfWork` gọi `SaveChanges`.** Controller, handler, repository, service đều không gọi.
3. **Không viết lại Identity**: vẫn dùng `UserManager`/`RoleManager`, chỉ thay đổi cách persist.
4. **Một `DbContext` cho Identity và nghiệp vụ** để commit nguyên tử; ranh giới giữa các phần thể hiện bằng schema.
5. **Id sinh ở application** (UUIDv7).
6. **Logical reference** từ dữ liệu nghiệp vụ tới user, không dùng FK. Không hard-delete user (đã có `IsActive`).
7. **Commit trước khi gửi response.**
8. **Controller mỏng**: chỉ gọi `IMediator`, không chạm `DbContext` hay `UserManager`.
9. **Command chỉ trả `Id`**. Dữ liệu đầy đủ (gồm audit) lấy qua query sau khi đã commit.

---

## 7. Ưu điểm

- Thay đổi của Identity và nghiệp vụ được commit **nguyên tử**: không còn cảnh user đã tạo nhưng bước sau thì lỗi.
- Muốn biết dữ liệu được lưu khi nào, chỉ cần nhìn **một chỗ** là Mediator/UoW.
- Giữ nguyên toàn bộ cơ chế bảo mật của Identity.
- Lỗi giữa chừng không để lại dữ liệu dở dang. Rollback có sẵn vì chưa có gì được ghi.
- Handler, repository và service đơn giản, dễ test, không phải lo transaction.
- Thêm use case mới chỉ cần thêm Command + Handler; mediator tự đăng ký và tự commit.

## 8. Nhược điểm

| Nhược điểm | Ghi chú |
|---|---|
| Ngữ nghĩa `IdentityResult` thay đổi | `Succeeded` chưa có nghĩa là đã lưu, dễ hiểu nhầm. |
| Phải override `UpdateAsync` của store | Tạo rồi cập nhật cùng một user trong một UoW sẽ biến INSERT thành UPDATE nếu dùng store mặc định (xem mục 9). |
| Query trong cùng UoW không thấy dữ liệu chưa commit | Chỉ `FindAsync` kiểm tra ChangeTracker trước; `RoleExistsAsync` và các query khác đi thẳng xuống DB. Unique index ở DB là lớp bảo vệ cuối. |
| Lỗi constraint chỉ lộ ra lúc commit | Ví dụ này kiểm tra trùng trước (`GetProductByNoAsync`). Hệ thống lớn nên map `DbUpdateException` tập trung. |
| Không có FK tới user | DB không đảm bảo toàn vẹn tham chiếu; application phải tự kiểm tra. |
| Mediator dùng reflection | Đổi lại không phụ thuộc thư viện ngoài (MediatR đã chuyển sang license thương mại). |
| Login không có lockout | `CheckPasswordAsync` không ghi gì nên login là query. Nếu bật lockout, login phải thành command và phải commit **cả khi thất bại** (để lưu số lần sai). |
| Role nằm trong JWT | Đổi role chỉ có hiệu lực từ lần login sau. |

---

## 9. Cạm bẫy đã xử lý

- **`Added` bị biến thành `Modified`.** Khi tắt auto-save, `CreateAsync` rồi `AddToRoleAsync` trên cùng một user sẽ khiến `UserStore.UpdateAsync` mặc định gọi `Attach/Update`. Kết quả: EF sinh `UPDATE identity.users` cho dòng chưa tồn tại, và insert `user_roles` vi phạm FK. Đã sửa bằng cách override `UpdateAsync`: nếu entity còn `Added` thì chỉ làm mới `ConcurrencyStamp`.
- **Tên FK còn sót `asp_net_*`.** Identity cấu hình quan hệ khi bảng còn tên `AspNet*`. `AppDbContext.RenameForeignKeys` đặt lại tên FK theo tên bảng mới.
- **Seed cần commit 2 lần.** Đây là ngoại lệ có chủ đích: Identity tra role trong DB, nên role phải được commit trước khi gán cho admin.
- **Log `fail` ở lần migrate đầu tiên.** Trên DB trống, EF đọc `__ef_migrations_history` trước khi tạo bảng này. Hành vi bình thường, chỉ xuất hiện một lần.

---

## 10. API

| Method | Route | Quyền |
|---|---|---|
| POST | `/api/auth/register` | anonymous |
| POST | `/api/auth/login` | anonymous |
| GET | `/api/products?pageIndex=&pageSize=&search=` | đã đăng nhập |
| GET | `/api/products/{id}` | đã đăng nhập |
| POST | `/api/products` | `Admin` |
| PUT | `/api/products/{id}` | `Admin` |
| DELETE | `/api/products/{id}` | `Admin` |

Định dạng response:

```json
{ "isSucceeded": true, "message": "Success", "data": { } }
{ "isSucceeded": false, "message": "Product No: P001 already exists.", "errors": [] }
```

## 11. Cấu hình và migration

| Key | Ý nghĩa |
|---|---|
| `ConnectionStrings:DefaultConnectionString` | Chuỗi kết nối PostgreSQL |
| `JwtSettings:Issuer` / `Audience` / `SecretKey` / `ExpiresInMinutes` | JWT; `SecretKey` ≥ 32 ký tự, bắt buộc |
| `Seed:AdminEmail` / `Seed:AdminPassword` | Tài khoản admin khởi tạo (để trống thì bỏ qua) |

```bash
dotnet ef migrations add <Name> --project src/IdentityUoW.Api --output-dir Persistence/Migrations
```
