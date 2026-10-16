using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.RTSPayment;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RTSPaymentController : ControllerBase
{
    private readonly IRTSPaymentService _paymentService;
    private readonly ILogger<RTSPaymentController> _logger;

    public RTSPaymentController(IRTSPaymentService paymentService, ILogger<RTSPaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Creates or retrieves a payment gateway order for the application's stored
    /// pending payment snapshot.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("create-order")]
    [ProducesResponseType(typeof(ApiResponse<PaymentOrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrder([FromBody] CreatePaymentOrderRequestDto request, CancellationToken ct)
    {
        if (request == null || request.ApplicationId <= 0)
        {
            return BadRequest(new ApiResponse<PaymentOrderResponseDto>
            {
                Success = false,
                Message = "Valid ApplicationId is required."
            });
        }

        try
        {
            var result = await _paymentService.CreatePaymentOrderAsync(request, ct);
            return Ok(new ApiResponse<PaymentOrderResponseDto>
            {
                Success = true,
                Message = "Payment order created successfully.",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment order for application {AppId}", request.ApplicationId);
            return BadRequest(new ApiResponse<PaymentOrderResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Creates or updates a statutory fee demand before payment (e.g. from rule engine fee assessment)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("demand")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDemandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrUpdateDemand([FromBody] CreatePaymentDemandRequestDto request, CancellationToken ct)
    {
        if (request == null || request.ApplicationId <= 0)
        {
            return BadRequest(new ApiResponse<PaymentDemandDto>
            {
                Success = false,
                Message = "Valid ApplicationId is required."
            });
        }

        try
        {
            var result = await _paymentService.CreateOrUpdatePaymentDemandAsync(request, ct);
            return Ok(new ApiResponse<PaymentDemandDto>
            {
                Success = true,
                Message = "Statutory fee demand assessed and saved successfully.",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fee demand for application {AppId}", request.ApplicationId);
            return BadRequest(new ApiResponse<PaymentDemandDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets the statutory fee demand for an application
    /// </summary>
    [AllowAnonymous]
    [HttpGet("demand/{applicationId:int}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDemandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDemand(int applicationId, CancellationToken ct)
    {
        var demand = await _paymentService.GetPaymentDemandAsync(applicationId, ct);
        if (demand == null)
        {
            return NotFound(new ApiResponse<PaymentDemandDto>
            {
                Success = false,
                Message = $"No fee demand found for application {applicationId}."
            });
        }

        return Ok(new ApiResponse<PaymentDemandDto>
        {
            Success = true,
            Message = "Statutory fee demand retrieved successfully.",
            Items = demand
        });
    }

    /// <summary>
    /// Creates the stored PENDING fee snapshot for an application when one does
    /// not already exist. This endpoint is idempotent and is primarily used to
    /// recover legacy applications submitted before payment snapshots existed.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("initialize-pending/{applicationId:int}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitializePending(int applicationId, CancellationToken ct)
    {
        try
        {
            var status = await _paymentService.InitializePendingPaymentAsync(applicationId, ct);
            return Ok(new ApiResponse<PaymentStatusDto>
            {
                Success = true,
                Message = "Payment snapshot is ready.",
                Items = status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing payment snapshot for application {AppId}", applicationId);
            return BadRequest(new ApiResponse<PaymentStatusDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Cryptographically verifies the payment signature from the gateway and generates an official receipt
    /// </summary>
    [AllowAnonymous]
    [HttpPost("verify-payment")]
    [ProducesResponseType(typeof(ApiResponse<VerifyPaymentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequestDto request, CancellationToken ct)
    {
        if (request == null || request.ApplicationId <= 0)
        {
            return BadRequest(new ApiResponse<VerifyPaymentResponseDto>
            {
                Success = false,
                Message = "Invalid payment verification payload."
            });
        }

        try
        {
            var result = await _paymentService.VerifyPaymentAsync(request, ct);
            return Ok(new ApiResponse<VerifyPaymentResponseDto>
            {
                Success = result.Success,
                Message = result.Message,
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment for application {AppId}", request.ApplicationId);
            return BadRequest(new ApiResponse<VerifyPaymentResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Records an offline municipal counter payment (Cash/Cheque/DD/POS/Challan) by authorized counter officer
    /// </summary>
    [AllowAnonymous]
    [HttpPost("record-offline")]
    [ProducesResponseType(typeof(ApiResponse<PaymentReceiptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordOfflinePayment([FromBody] RecordOfflinePaymentRequestDto request, CancellationToken ct)
    {
        if (request == null || request.ApplicationId <= 0)
        {
            return BadRequest(new ApiResponse<PaymentReceiptDto>
            {
                Success = false,
                Message = "Invalid offline payment request. Valid ApplicationId is required."
            });
        }

        try
        {
            // Extract user id from token or claim if authenticated, fallback to 1
            int userId = 1;
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("id") ?? User.FindFirst("sub");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedId))
            {
                userId = parsedId;
            }

            var receipt = await _paymentService.RecordOfflinePaymentAsync(request, userId, ct);
            return Ok(new ApiResponse<PaymentReceiptDto>
            {
                Success = true,
                Message = "Offline counter payment recorded successfully.",
                Items = receipt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording offline payment for application {AppId}", request.ApplicationId);
            return BadRequest(new ApiResponse<PaymentReceiptDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets payment receipt details for a paid application
    /// </summary>
    [AllowAnonymous]
    [HttpGet("receipt/{applicationId}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentReceiptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReceipt(int applicationId, CancellationToken ct)
    {
        var receipt = await _paymentService.GetPaymentReceiptByApplicationIdAsync(applicationId, ct);
        if (receipt == null)
        {
            return NotFound(new ApiResponse<PaymentReceiptDto>
            {
                Success = false,
                Message = "No successful payment receipt found for this application."
            });
        }

        return Ok(new ApiResponse<PaymentReceiptDto>
        {
            Success = true,
            Message = "Payment receipt retrieved successfully.",
            Items = receipt
        });
    }

    /// <summary>
    /// Gets current payment status and fee requirement for an application
    /// </summary>
    [AllowAnonymous]
    [HttpGet("status/{applicationId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(int applicationId, CancellationToken ct)
    {
        var status = await _paymentService.GetPaymentStatusAsync(applicationId, ct);
        if (status == null)
        {
            return NotFound(new { success = false, message = "Application not found." });
        }

        return Ok(new { success = true, data = status });
    }

    /// <summary>
    /// Gets the latest payment snapshot for many application numbers in one request.
    /// Applications owned only by an external legacy system are omitted from the result.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStatuses([FromBody] BulkPaymentStatusRequestDto request, CancellationToken ct)
    {
        if (request?.ApplicationNos == null || request.ApplicationNos.Count == 0)
        {
            return BadRequest(new ApiResponse<List<PaymentStatusDto>>
            {
                Success = false,
                Message = "At least one application number is required."
            });
        }

        var statuses = await _paymentService.GetPaymentStatusesByApplicationNosAsync(request.ApplicationNos, ct);
        return Ok(new ApiResponse<List<PaymentStatusDto>>
        {
            Success = true,
            Message = "Payment statuses retrieved successfully.",
            Items = statuses.ToList()
        });
    }

    /// <summary>
    /// Gets payment receipt details by receipt number
    /// </summary>
    [AllowAnonymous]
    [HttpGet("receipt-by-no/{*receiptNo}")]
    [HttpGet("receipt-by-no")]
    [ProducesResponseType(typeof(ApiResponse<PaymentReceiptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReceiptByNo(
        [FromRoute] string? receiptNo,
        [FromQuery] string? rNo,
        [FromQuery] string? receiptNumber,
        CancellationToken ct)
    {
        var targetNo = !string.IsNullOrWhiteSpace(receiptNo) ? receiptNo : (!string.IsNullOrWhiteSpace(rNo) ? rNo : receiptNumber);
        if (string.IsNullOrWhiteSpace(targetNo))
        {
            return BadRequest(new ApiResponse<PaymentReceiptDto>
            {
                Success = false,
                Message = "Valid receipt number is required."
            });
        }

        targetNo = Uri.UnescapeDataString(targetNo).Trim();

        var receipt = await _paymentService.GetPaymentReceiptByReceiptNoAsync(targetNo, ct);
        if (receipt == null)
        {
            return NotFound(new ApiResponse<PaymentReceiptDto>
            {
                Success = false,
                Message = $"No payment receipt found with receipt number '{targetNo}'."
            });
        }

        return Ok(new ApiResponse<PaymentReceiptDto>
        {
            Success = true,
            Message = "Payment receipt retrieved successfully.",
            Items = receipt
        });
    }

    /// <summary>
    /// Gets paginated payment transactions with comprehensive filtering (Department, Service, Mode, Status, Date)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("transactions")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PaymentTransactionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions([FromBody] PaymentTransactionQueryDto query, CancellationToken ct)
    {
        var result = await _paymentService.GetTransactionsAsync(query ?? new PaymentTransactionQueryDto(), ct);
        return Ok(new ApiResponse<PagedResult<PaymentTransactionListItemDto>>
        {
            Success = true,
            Message = "Payment transactions retrieved successfully.",
            Items = result
        });
    }

    /// <summary>
    /// Webhook handler for asynchronous Razorpay gateway event notifications
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook(CancellationToken ct)
    {
        try
        {
            using var reader = new System.IO.StreamReader(Request.Body, System.Text.Encoding.UTF8);
            var payload = await reader.ReadToEndAsync(ct);
            var signature = Request.Headers["X-Razorpay-Signature"].ToString();

            if (string.IsNullOrWhiteSpace(payload))
            {
                return BadRequest(new { success = false, message = "Empty payload received." });
            }

            var processed = await _paymentService.ProcessWebhookAsync(payload, signature, ct);
            return Ok(new { success = true, processed });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Razorpay Webhook");
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Gets pending clearance / settlement payment transactions (Cheque, DD, Online) with pagination and filters
    /// </summary>
    [AllowAnonymous]
    [HttpGet("pending-clearance")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PendingClearanceItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingClearance(
        [FromQuery] string? paymentMode,
        [FromQuery] string? statusCode,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? searchText,
        [FromQuery] int? departmentId,
        [FromQuery] int? serviceId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = new PendingClearanceQueryDto
        {
            PaymentMode = paymentMode,
            StatusCode = statusCode,
            FromDate = fromDate,
            ToDate = toDate,
            SearchText = searchText,
            DepartmentId = departmentId,
            ServiceId = serviceId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _paymentService.GetPendingClearancePaymentsAsync(query, ct);
        return Ok(new ApiResponse<PagedResult<PendingClearanceItemDto>>
        {
            Success = true,
            Message = "Pending clearance transactions retrieved successfully.",
            Items = result
        });
    }

    /// <summary>
    /// Gets summary statistics for Cheque/DD/Online clearance settlement dashboard
    /// </summary>
    [AllowAnonymous]
    [HttpGet("clearance-stats")]
    [ProducesResponseType(typeof(ApiResponse<PendingClearanceStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClearanceStats(CancellationToken ct)
    {
        var stats = await _paymentService.GetPendingClearanceStatsAsync(ct);
        return Ok(new ApiResponse<PendingClearanceStatsDto>
        {
            Success = true,
            Message = "Clearance statistics retrieved successfully.",
            Items = stats
        });
    }

    /// <summary>
    /// Approves and clears a Cheque, DD, or Online transaction (marks SUCCESS, generates official receipt, advances application)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("approve-clearance")]
    [ProducesResponseType(typeof(ApiResponse<VerifyPaymentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveClearance([FromBody] ApprovePaymentRequestDto request, CancellationToken ct)
    {
        if (request == null || request.TransactionId <= 0)
        {
            return BadRequest(new ApiResponse<VerifyPaymentResponseDto>
            {
                Success = false,
                Message = "Valid TransactionId is required."
            });
        }

        try
        {
            int userId = 1;
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("id") ?? User.FindFirst("sub");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedId)) userId = parsedId;

            var result = await _paymentService.ApproveTransactionAsync(request, userId, ct);
            return Ok(new ApiResponse<VerifyPaymentResponseDto>
            {
                Success = true,
                Message = result.Message,
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving payment clearance for transaction {TxnId}", request.TransactionId);
            return BadRequest(new ApiResponse<VerifyPaymentResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Disapproves a Cheque/DD or Online transaction (marks BOUNCED / DISAPPROVED, reverts application to Payment Pending)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("disapprove-clearance")]
    [ProducesResponseType(typeof(ApiResponse<VerifyPaymentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DisapproveClearance([FromBody] DisapprovePaymentRequestDto request, CancellationToken ct)
    {
        if (request == null || request.TransactionId <= 0 || string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new ApiResponse<VerifyPaymentResponseDto>
            {
                Success = false,
                Message = "TransactionId and mandatory rejection reason are required."
            });
        }

        try
        {
            int userId = 1;
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("id") ?? User.FindFirst("sub");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedId)) userId = parsedId;

            var result = await _paymentService.DisapproveTransactionAsync(request, userId, ct);
            return Ok(new ApiResponse<VerifyPaymentResponseDto>
            {
                Success = true,
                Message = result.Message,
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disapproving payment clearance for transaction {TxnId}", request.TransactionId);
            return BadRequest(new ApiResponse<VerifyPaymentResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Cancels / Voids an issued payment receipt with mandatory reason (marks CANCELLED, reverts application to Payment Pending)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("cancel-receipt")]
    [ProducesResponseType(typeof(ApiResponse<VerifyPaymentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelReceipt([FromBody] CancelReceiptRequestDto request, CancellationToken ct)
    {
        if (request == null || request.TransactionId <= 0 || string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new ApiResponse<VerifyPaymentResponseDto>
            {
                Success = false,
                Message = "TransactionId and mandatory cancellation reason are required."
            });
        }

        try
        {
            int userId = 1;
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("id") ?? User.FindFirst("sub");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedId)) userId = parsedId;

            var result = await _paymentService.CancelReceiptAsync(request, userId, ct);
            return Ok(new ApiResponse<VerifyPaymentResponseDto>
            {
                Success = true,
                Message = result.Message,
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling receipt for transaction {TxnId}", request.TransactionId);
            return BadRequest(new ApiResponse<VerifyPaymentResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Bulk upload bank clearance scroll / statement Excel file (.xlsx) to match and clear or bounce payments
    /// </summary>
    [AllowAnonymous]
    [HttpPost("bulk-clearance-upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<BulkClearanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkClearanceUpload([FromForm] BulkClearanceUploadRequestDto request, CancellationToken ct)
    {
        if (request?.File == null || request.File.Length == 0)
        {
            return BadRequest(new ApiResponse<BulkClearanceResponseDto>
            {
                Success = false,
                Message = "Excel file (.xlsx) is required for bulk clearance upload."
            });
        }

        try
        {
            int userId = 1;
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("id") ?? User.FindFirst("sub");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedId)) userId = parsedId;

            using var stream = request.File.OpenReadStream();
            var result = await _paymentService.BulkClearanceUploadAsync(stream, userId, ct);

            return Ok(new ApiResponse<BulkClearanceResponseDto>
            {
                Success = true,
                Message = $"Bulk clearance completed. Success: {result.SuccessCount}, Failed: {result.FailureCount}",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk clearance Excel upload");
            return BadRequest(new ApiResponse<BulkClearanceResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Downloads an Excel template (.xlsx) for bulk bank payment clearance
    /// </summary>
    [AllowAnonymous]
    [HttpGet("clearance-template")]
    public async Task<IActionResult> DownloadClearanceTemplate(CancellationToken ct)
    {
        try
        {
            var bytes = await _paymentService.DownloadClearanceTemplateAsync(ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Payment_Clearance_Template.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating clearance template");
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Gets list of scheduled and recognized banks for payment instruments
    /// </summary>
    [AllowAnonymous]
    [HttpGet("banks")]
    [ProducesResponseType(typeof(ApiResponse<List<RTSBankLookupDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBanks(CancellationToken ct)
    {
        try
        {
            var banks = await _paymentService.GetBanksAsync(ct);
            return Ok(new ApiResponse<List<RTSBankLookupDto>>
            {
                Success = true,
                Message = "Banks fetched successfully.",
                Items = banks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching banks");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<List<RTSBankLookupDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets list of active payment modes from master lookup
    /// </summary>
    [AllowAnonymous]
    [HttpGet("payment-modes")]
    [ProducesResponseType(typeof(ApiResponse<List<RTSPaymentModeLookupDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentModes(CancellationToken ct)
    {
        try
        {
            var modes = await _paymentService.GetPaymentModesAsync(ct);
            return Ok(new ApiResponse<List<RTSPaymentModeLookupDto>>
            {
                Success = true,
                Message = "Payment modes fetched successfully.",
                Items = modes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment modes");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<List<RTSPaymentModeLookupDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets audit log of cancelled receipts
    /// </summary>
    [AllowAnonymous]
    [HttpGet("cancellation-logs")]
    [ProducesResponseType(typeof(ApiResponse<List<ReceiptCancellationLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCancellationLogs([FromQuery] int? applicationId, [FromQuery] long? transactionId, CancellationToken ct)
    {
        try
        {
            var logs = await _paymentService.GetCancellationLogsAsync(applicationId, transactionId, ct);
            return Ok(new ApiResponse<List<ReceiptCancellationLogDto>>
            {
                Success = true,
                Message = "Cancellation audit logs fetched successfully.",
                Items = logs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching receipt cancellation audit logs");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<List<ReceiptCancellationLogDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Exports payment clearance settlement records to Excel (.xlsx) or CSV format
    /// </summary>
    [AllowAnonymous]
    [HttpGet("export-clearance")]
    public async Task<IActionResult> ExportClearance(
        [FromQuery] string? paymentMode,
        [FromQuery] string? statusCode,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? searchText,
        [FromQuery] string format = "excel",
        CancellationToken ct = default)
    {
        try
        {
            var query = new PendingClearanceQueryDto
            {
                PaymentMode = paymentMode,
                StatusCode = statusCode,
                FromDate = fromDate,
                ToDate = toDate,
                SearchText = searchText,
                PageNumber = 1,
                PageSize = 10000
            };

            var result = await _paymentService.GetPendingClearancePaymentsAsync(query, ct);
            var records = result?.Items?.ToList() ?? new List<PendingClearanceItemDto>();

            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                sb.AppendLine("Application No,Citizen Name,Receipt No,Payment Mode,Bank Name,Instrument No,Instrument Date,Amount (Rs.),Status,Created Date");

                foreach (var item in records)
                {
                    var appNo = $"\"{item.ApplicationNo?.Replace("\"", "\"\"")}\"";
                    var citizen = $"\"{item.CitizenName?.Replace("\"", "\"\"")}\"";
                    var receipt = $"\"{item.ReceiptNo?.Replace("\"", "\"\"")}\"";
                    var mode = $"\"{item.PaymentMode?.Replace("\"", "\"\"")}\"";
                    var bank = $"\"{item.BankName?.Replace("\"", "\"\"")}\"";
                    var instNo = $"\"{item.InstrumentNo?.Replace("\"", "\"\"")}\"";
                    var instDate = item.InstrumentDate.HasValue ? item.InstrumentDate.Value.ToString("yyyy-MM-dd") : "";
                    var amount = item.TotalAmount.ToString("F2");
                    var status = $"\"{(item.ApprovalStatus ?? item.PaymentStatus)?.Replace("\"", "\"\"")}\"";
                    var createdDate = item.CreatedDate.ToString("yyyy-MM-dd HH:mm");

                    sb.AppendLine($"{appNo},{citizen},{receipt},{mode},{bank},{instNo},{instDate},{amount},{status},{createdDate}");
                }

                var bom = Encoding.UTF8.GetPreamble();
                var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
                var combined = bom.Concat(csvBytes).ToArray();

                var fileName = $"Payment_Settlement_Clearance_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                return File(combined, "text/csv; charset=utf-8", fileName);
            }
            else
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Settlement_Report");

                // Municipal Header Banner
                ws.Cell(1, 1).Value = "AKOLA MUNICIPAL CORPORATION - RIGHT TO SERVICES (RTS)";
                ws.Range(1, 1, 1, 10).Merge();
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 14;
                ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
                ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(30, 58, 138); // Deep Navy
                ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(2, 1).Value = $"Payment Settlement & Clearance Report | Generated: {DateTime.UtcNow:dd-MMM-yyyy HH:mm} UTC | Total Records: {records.Count}";
                ws.Range(2, 1, 2, 10).Merge();
                ws.Cell(2, 1).Style.Font.Italic = true;
                ws.Cell(2, 1).Style.Font.FontSize = 10;
                ws.Cell(2, 1).Style.Font.FontColor = XLColor.White;
                ws.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(49, 46, 129); // Indigo
                ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Table Column Headers
                var headers = new[]
                {
                    "Sr. No.",
                    "Application No",
                    "Citizen Name",
                    "Receipt No",
                    "Payment Mode",
                    "Bank Name",
                    "Instrument / Cheque No",
                    "Instrument Date",
                    "Amount (Rs.)",
                    "Status"
                };

                for (int c = 0; c < headers.Length; c++)
                {
                    var cell = ws.Cell(4, c + 1);
                    cell.Value = headers[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(15, 23, 42); // Slate 900
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                int row = 5;
                decimal totalAmount = 0;
                for (int i = 0; i < records.Count; i++)
                {
                    var item = records[i];
                    totalAmount += item.TotalAmount;

                    ws.Cell(row, 1).Value = i + 1;
                    ws.Cell(row, 2).Value = item.ApplicationNo ?? "-";
                    ws.Cell(row, 3).Value = item.CitizenName ?? "-";
                    ws.Cell(row, 4).Value = item.ReceiptNo ?? "-";
                    ws.Cell(row, 5).Value = item.PaymentMode ?? "-";
                    ws.Cell(row, 6).Value = item.BankName ?? "-";
                    ws.Cell(row, 7).Value = item.InstrumentNo ?? "-";
                    ws.Cell(row, 8).Value = item.InstrumentDate.HasValue ? item.InstrumentDate.Value.ToString("dd-MMM-yyyy") : "-";
                    ws.Cell(row, 9).Value = (double)item.TotalAmount;
                    ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 10).Value = item.ApprovalStatus ?? item.PaymentStatus ?? "-";

                    // Zebra striping
                    if (i % 2 == 1)
                    {
                        ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252);
                    }

                    row++;
                }

                // Summary Row
                ws.Cell(row, 1).Value = "TOTAL";
                ws.Range(row, 1, row, 8).Merge();
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(row, 9).Value = (double)totalAmount;
                ws.Cell(row, 9).Style.Font.Bold = true;
                ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
                ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(226, 232, 240);

                ws.Columns().AdjustToContents();

                using var ms = new MemoryStream();
                workbook.SaveAs(ms);
                var fileName = $"Payment_Settlement_Clearance_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting payment clearance report");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<string>
            {
                Success = false,
                Message = "Error generating export: " + ex.Message
            });
        }
    }
}
