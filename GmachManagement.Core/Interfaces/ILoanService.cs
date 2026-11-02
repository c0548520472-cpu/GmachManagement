using System;
using System.Collections.Generic;
using System.Text;

using GmachManagement.Core.Entities;

namespace GmachManagement.Core.Interfaces
{
    public interface ILoanService
    {

        Task<List<Loan>> GetActiveLoansAsync(
            CancellationToken cancellationToken = default);

        Task<List<Loan>> GetOverdueLoansAsync(
            CancellationToken cancellationToken = default);

        Task<List<Loan>> GetActiveLoansByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<List<Loan>> GetMyLoansAsync(
            int userId,
            CancellationToken cancellationToken = default);


        Task<bool> ReturnItemAsync(
            int loanId,
            int userId,
            CancellationToken cancellationToken = default);

        Task<Loan?> BorrowItemAsync(
            int userId,
            int itemCopyId,
            DateTime expectedReturnDate,
            CancellationToken cancellationToken = default);
           Task<List<Loan>> GetAllLoansAsync(
          CancellationToken cancellationToken = default);
    }
}

