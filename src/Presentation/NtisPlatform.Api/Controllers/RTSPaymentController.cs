using System;
using System.Threading;
using System.Threading.Tasks;
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
    /// Creates a payment gateway order for an RTS application (amount dynamically resolved from service master)
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
    /// Gets payment receipt details by receipt number
    /// </summary>
    [AllowAnonymous]
    [HttpGet("receipt-by-no/{receiptNo}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentReceiptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReceiptByNo(string receiptNo, CancellationToken ct)
    {
        var receipt = await _paymentService.GetPaymentReceiptByReceiptNoAsync(receiptNo, ct);
        if (receipt == null)
        {
            return NotFound(new ApiResponse<PaymentReceiptDto>
            {
                Success = false,
                Message = $"No payment receipt found with receipt number '{receiptNo}'."
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
}
