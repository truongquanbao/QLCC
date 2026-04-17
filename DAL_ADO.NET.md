Viết Data Access Layer bằng ADO.NET:

Yêu cầu:

- SqlConnection
- SqlCommand
- Parameterized query
- Chống SQL injection

Tạo class:

UserDAL:
- GetByUsername
- Insert
- Update
- LockUser

InvoiceDAL:
- CreateInvoice
- GetByApartment

Có try-catch + logging.