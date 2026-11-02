using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;

namespace GmachManagement.Service.Services
{
    public class LoanService : ILoanService
    {
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

