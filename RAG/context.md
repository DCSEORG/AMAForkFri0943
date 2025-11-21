# Expense Management System - Context Information

## Database Schema

The Expense Management System uses the following database schema:

### Tables:
- **Roles**: Stores user roles (Employee, Manager)
- **Users**: Stores user information with role assignments
- **ExpenseCategories**: Categories for expenses (Travel, Meals, Supplies, Accommodation, Other)
- **ExpenseStatus**: Status values (Draft, Submitted, Approved, Rejected)
- **Expenses**: Main table storing expense records

### Key Fields:
- Expenses use AmountMinor to store amounts in pence (e.g., £12.34 is stored as 1234 pence)
- Currency is always GBP (British Pounds)
- Status workflow: Draft → Submitted → Approved/Rejected

## API Endpoints

### Expenses API (`/api/expenses`)
- GET: List all expenses (filter by userId, statusId)
- GET /{id}: Get specific expense
- POST: Create new expense
- PUT /{id}: Update expense
- DELETE /{id}: Delete expense
- POST /{id}/submit: Submit expense for approval
- POST /{id}/approve: Approve expense (requires reviewerId)
- POST /{id}/reject: Reject expense (requires reviewerId)

### Categories API (`/api/categories`)
- GET: List all categories

### Statuses API (`/api/statuses`)
- GET: List all statuses

### Users API (`/api/users`)
- GET: List all users

## Common Operations

1. **Creating an Expense**: POST to /api/expenses with JSON body including UserId, CategoryId, StatusId, AmountMinor, ExpenseDate, Description
2. **Submitting for Approval**: POST to /api/expenses/{id}/submit
3. **Approving/Rejecting**: POST to /api/expenses/{id}/approve or /api/expenses/{id}/reject with reviewerId parameter

## Example Queries
- "Show me all pending expenses"
- "What's the total amount of approved expenses?"
- "Create a new expense for £50 taxi ride"
- "Approve expense ID 5"
