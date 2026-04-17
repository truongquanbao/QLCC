Viết stored procedure sp_SeedInitialData:

Yêu cầu:

- Tạo:
  + superadmin
  + manager1
  + resident1
- Tạo:
  + 1 building → 2 block → 5 tầng → 4 căn/tầng
- Tạo fee types
- Tạo 3–5 invoices
- Tạo complaints mẫu

Kiểm tra flag IsSeeded trong SystemConfig trước khi seed.

Dùng TRANSACTION.