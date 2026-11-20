using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;

namespace GmachManagement.Service.Services
{
    public class LoanService : ILoanService
    {
        public Task<List<Loan>> GetOwnerLoansAsync(int ownerId, bool activeOnly, bool overdueOnly, CancellationToken ct)
            => _loanRepository.GetOwnerLoansAsync(ownerId, activeOnly, overdueOnly, ct);
        public Task<bool> ReturnAsAdminAsync(int loanId, CancellationToken ct) => _loanRepository.ReturnAsAdminAsync(loanId, ct);
        public Task<bool> ChangeReturnDateAsync(int loanId, DateTime date, CancellationToken ct)
        {
            if (date <= DateTime.UtcNow)
                throw new ArgumentOutOfRangeException(nameof(date), "תאריך ההחזרה חייב להיות בעתיד");

            return _loanRepository.ChangeReturnDateAsync(loanId, date, ct);
        }
        private readonly ILoanRepository _loanRepository;

        public LoanService(ILoanRepository loanRepository)
        {
            _loanRepository = loanRepository;
        }

        public async Task<List<Loan>> GetActiveLoansAsync(
            CancellationToken cancellationToken = default)
        {
            return await _loanRepository.GetActiveLoansAsync(
                cancellationToken);
        }

        public async Task<List<Loan>> GetOverdueLoansAsync(
            CancellationToken cancellationToken = default)
        {
            return await _loanRepository.GetOverdueLoansAsync(
                cancellationToken);
        }

        public async Task<List<Loan>> GetActiveLoansByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await _loanRepository.GetActiveLoansByUserIdAsync(
                userId,
                cancellationToken);
        }


        public async Task<List<Loan>> GetMyLoansAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await _loanRepository.GetLoansByUserIdAsync(
                userId,
                cancellationToken);
        }

        public async Task<List<Loan>> GetAllLoansAsync(
            CancellationToken cancellationToken = default)
        {
            return await _loanRepository.GetAllLoansAsync(
                cancellationToken);
        }



        public async Task<Loan?> BorrowItemAsync(
            int userId,
            int itemCopyId,
            DateTime expectedReturnDate,
            CancellationToken cancellationToken = default)
        {
            return await _loanRepository.BorrowItemAsync(
                userId,
                itemCopyId,
                expectedReturnDate,
                cancellationToken);
        }

        public async Task<bool> ReturnItemAsync(
            int loanId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await _loanRepository.ReturnItemAsync(
                loanId,
                userId,
                cancellationToken);
        }

    }
}

