
using AutoMapper;
using GmachManagement.Core.DTOs;
using GmachManagement.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GmachManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class LoansController : ControllerBase
    {
        private readonly ILoanService _loanService;
        private readonly IMapper _mapper;

        public LoansController(
            ILoanService loanService,
            IMapper mapper)
        {
            _loanService = loanService;
            _mapper = mapper;
        }


        [HttpGet("active")]
        [Authorize(Roles = "GmachAdmin")]
        public async Task<ActionResult<List<LoanDto>>> GetActiveLoans(
            CancellationToken cancellationToken)
        {
            return Forbid();
            var loans = await _loanService.GetActiveLoansAsync(
                cancellationToken);

            return Ok(_mapper.Map<List<LoanDto>>(loans));
        }

        [HttpGet("overdue")]
        [Authorize(Roles = "GmachAdmin")]
        public async Task<ActionResult<List<LoanDto>>> GetOverdueLoans(
            CancellationToken cancellationToken)
        {
            return Forbid();
            var loans = await _loanService.GetOverdueLoansAsync(
                cancellationToken);

            return Ok(_mapper.Map<List<LoanDto>>(loans));
        }

        [HttpGet("my/active")]
        public async Task<ActionResult<List<LoanDto>>> GetMyActiveLoans(
            CancellationToken cancellationToken)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId) || userId <= 0)
            {
                return Unauthorized("לא ניתן לזהות את המשתמש המחובר");
            }

            var loans = await _loanService.GetActiveLoansByUserIdAsync(
                userId,
                cancellationToken);

            return Ok(_mapper.Map<List<LoanDto>>(loans));
        }



        [HttpGet("my")]
        public async Task<ActionResult<List<LoanDto>>> GetMyLoans(
            CancellationToken cancellationToken)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId) || userId <= 0)
            {
                return Unauthorized("לא ניתן לזהות את המשתמש המחובר");
            }

            var loans = await _loanService.GetMyLoansAsync(
                userId,
                cancellationToken);

            var result = _mapper.Map<List<LoanDto>>(loans);

            return Ok(result);
        }



        [HttpGet]
        [Authorize(Roles = "GmachAdmin")]
        public async Task<ActionResult<List<LoanDto>>> GetAll(
            CancellationToken cancellationToken)
        {
            return Forbid();
            var loans = await _loanService.GetAllLoansAsync(
                cancellationToken);

            var result = _mapper.Map<List<LoanDto>>(loans);

            return Ok(result);
        }


        [HttpPost]
        public async Task<ActionResult<LoanDto>> BorrowItem(
            [FromBody] BorrowItemDto request,
            CancellationToken cancellationToken)
        {
            return StatusCode(410, new { message = "יש לשלוח בקשת השאלה במקום לבצע השאלה ישירה" });

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId) || userId <= 0)
            {
                return Unauthorized("לא ניתן לזהות את המשתמש המחובר");
            }

            if (request.ExpectedReturnDate <= DateTime.UtcNow)
            {
                return BadRequest("תאריך ההחזרה חייב להיות בעתיד");
            }

            var loan = await _loanService.BorrowItemAsync(
                userId,
                request.ItemCopyId,
                request.ExpectedReturnDate,
                cancellationToken);

            if (loan == null)
            {
                return NotFound("העותק המבוקש לא נמצא");
            }

            var result = _mapper.Map<LoanDto>(loan);

            return StatusCode(201, result);
        }

        [HttpPost("{loanId:int}/return")]
        public async Task<IActionResult> ReturnItem(
            int loanId,
            CancellationToken cancellationToken)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId) || userId <= 0)
            {
                return Unauthorized("לא ניתן לזהות את המשתמש המחובר");
            }

            var returned = await _loanService.ReturnItemAsync(
                loanId,
                userId,
                cancellationToken);

            if (!returned)
            {
                return NotFound("ההשאלה לא נמצאה");
            }

            return NoContent();
        }


    }
}
