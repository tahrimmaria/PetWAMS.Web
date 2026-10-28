using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PetWAMS.Web.Services
{
    // =========================================================
    // PAYMENT SESSION RESULT
    // =========================================================
    public class SslCommerzInitResult
    {
        public bool IsSuccess { get; set; }

        public string Status { get; set; } = string.Empty;

        public string FailedReason { get; set; } = string.Empty;

        public string SessionKey { get; set; } = string.Empty;

        public string GatewayPageUrl { get; set; } = string.Empty;
    }


    // =========================================================
    // PAYMENT VALIDATION RESULT
    // =========================================================
    public class SslCommerzValidationResult
    {
        public bool IsValid { get; set; }

        public string Status { get; set; } = string.Empty;

        public string TransactionId { get; set; } = string.Empty;

        public string ValidationId { get; set; } = string.Empty;

        public string BankTransactionId { get; set; } = string.Empty;

        public string CardType { get; set; } = string.Empty;

        public string CardIssuer { get; set; } = string.Empty;

        public string CardBrand { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public decimal StoreAmount { get; set; }

        public string Currency { get; set; } = string.Empty;

        public int RiskLevel { get; set; }

        public string RiskTitle { get; set; } = string.Empty;
    }


    // =========================================================
    // REAL REFUND INITIATION RESULT
    // =========================================================
    public class SslCommerzRefundResult
    {
        public bool IsSuccess { get; set; }

        public string ApiConnect { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string BankTransactionId { get; set; } = string.Empty;

        public string TransactionId { get; set; } = string.Empty;

        public string RefundReferenceId { get; set; } = string.Empty;

        public string ErrorReason { get; set; } = string.Empty;
    }


    // =========================================================
    // REAL REFUND STATUS RESULT
    // =========================================================
    public class SslCommerzRefundStatusResult
    {
        public bool IsSuccess { get; set; }

        public bool IsRefunded { get; set; }

        public bool IsProcessing { get; set; }

        public bool IsCancelled { get; set; }

        public string ApiConnect { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string BankTransactionId { get; set; } = string.Empty;

        public string TransactionId { get; set; } = string.Empty;

        public string RefundReferenceId { get; set; } = string.Empty;

        public string ErrorReason { get; set; } = string.Empty;

        public DateTime? InitiatedOn { get; set; }

        public DateTime? RefundedOn { get; set; }
    }


    // =========================================================
    // SSLCOMMERZ SERVICE
    // =========================================================
    public class SslCommerzService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;


        public SslCommerzService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }


        // =====================================================
        // CREATE SSLCOMMERZ PAYMENT SESSION
        // =====================================================
        public async Task<SslCommerzInitResult> CreatePaymentSessionAsync(
            string transactionId,
            decimal totalAmount,
            string customerName,
            string customerEmail,
            string customerPhone,
            string customerAddress,
            string baseUrl)
        {
            var result = new SslCommerzInitResult();

            try
            {
                string storeId = GetConfig("StoreId");
                string storePassword = GetConfig("StorePassword");
                string currency = GetConfig("Currency");

                string sessionUrl = GetSessionUrl();

                if (string.IsNullOrWhiteSpace(storeId) ||
                    string.IsNullOrWhiteSpace(storePassword))
                {
                    result.IsSuccess = false;
                    result.Status = "FAILED";
                    result.FailedReason =
                        "SSLCommerz Store ID or Store Password is missing.";

                    return result;
                }


                string successUrl =
                    $"{baseUrl}/PetCareShop/PaymentSuccess";

                string failUrl =
                    $"{baseUrl}/PetCareShop/PaymentFail";

                string cancelUrl =
                    $"{baseUrl}/PetCareShop/PaymentCancel";

                string ipnUrl =
                    $"{baseUrl}/PetCareShop/PaymentIpn";


                var postData = new Dictionary<string, string>
                {
                    { "store_id", storeId },

                    { "store_passwd", storePassword },

                    {
                        "total_amount",
                        totalAmount.ToString(
                            "0.00",
                            CultureInfo.InvariantCulture)
                    },

                    { "currency", currency },

                    { "tran_id", transactionId },


                    // CALLBACK URLS
                    { "success_url", successUrl },

                    { "fail_url", failUrl },

                    { "cancel_url", cancelUrl },

                    { "ipn_url", ipnUrl },


                    // CUSTOMER INFORMATION
                    {
                        "cus_name",
                        Safe(
                            customerName,
                            "PetHope Customer")
                    },

                    {
                        "cus_email",
                        Safe(
                            customerEmail,
                            "customer@pethope.com")
                    },

                    {
                        "cus_add1",
                        Safe(
                            customerAddress,
                            "Dhaka")
                    },

                    { "cus_add2", "" },

                    { "cus_city", "Dhaka" },

                    { "cus_state", "Dhaka" },

                    { "cus_postcode", "1200" },

                    { "cus_country", "Bangladesh" },

                    {
                        "cus_phone",
                        Safe(
                            customerPhone,
                            "01700000000")
                    },

                    { "cus_fax", "" },


                    // SHIPPING INFORMATION
                    {
                        "ship_name",
                        Safe(
                            customerName,
                            "PetHope Customer")
                    },

                    {
                        "ship_add1",
                        Safe(
                            customerAddress,
                            "Dhaka")
                    },

                    { "ship_add2", "" },

                    { "ship_city", "Dhaka" },

                    { "ship_state", "Dhaka" },

                    { "ship_postcode", "1200" },

                    { "ship_country", "Bangladesh" },


                    // PRODUCT INFORMATION
                    { "shipping_method", "YES" },

                    { "num_of_item", "1" },

                    {
                        "product_name",
                        "Pet Care Essentials"
                    },

                    {
                        "product_category",
                        "Pet Care"
                    },

                    {
                        "product_profile",
                        "general"
                    }
                };


                var client =
                    _httpClientFactory.CreateClient();


                using var content =
                    new FormUrlEncodedContent(postData);


                var response =
                    await client.PostAsync(
                        sessionUrl,
                        content);


                string json =
                    await response.Content
                        .ReadAsStringAsync();


                if (!response.IsSuccessStatusCode)
                {
                    result.IsSuccess = false;

                    result.Status = "FAILED";

                    result.FailedReason =
                        "SSLCommerz session API request failed.";

                    return result;
                }


                using var document =
                    JsonDocument.Parse(json);


                var root =
                    document.RootElement;


                result.Status =
                    GetString(
                        root,
                        "status");


                result.FailedReason =
                    GetString(
                        root,
                        "failedreason");


                result.SessionKey =
                    GetString(
                        root,
                        "sessionkey");


                result.GatewayPageUrl =
                    GetString(
                        root,
                        "GatewayPageURL");


                result.IsSuccess =
                    result.Status.Equals(
                        "SUCCESS",
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    !string.IsNullOrWhiteSpace(
                        result.GatewayPageUrl);


                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;

                result.Status = "FAILED";

                result.FailedReason =
                    "SSLCommerz session error: "
                    + ex.Message;

                return result;
            }
        }


        // =====================================================
        // VALIDATE SSLCOMMERZ PAYMENT
        // =====================================================
        public async Task<SslCommerzValidationResult>
            ValidatePaymentAsync(
                string validationId)
        {
            var result =
                new SslCommerzValidationResult();


            try
            {
                if (string.IsNullOrWhiteSpace(validationId))
                {
                    result.IsValid = false;

                    result.Status = "FAILED";

                    return result;
                }


                string storeId =
                    GetConfig("StoreId");


                string storePassword =
                    GetConfig("StorePassword");


                string validationUrl =
                    GetValidationUrl();


                string requestUrl =
                    validationUrl
                    + "?val_id="
                    + Uri.EscapeDataString(validationId)

                    + "&store_id="
                    + Uri.EscapeDataString(storeId)

                    + "&store_passwd="
                    + Uri.EscapeDataString(storePassword)

                    + "&format=json";


                var client =
                    _httpClientFactory.CreateClient();


                var response =
                    await client.GetAsync(requestUrl);


                string json =
                    await response.Content
                        .ReadAsStringAsync();


                if (!response.IsSuccessStatusCode)
                {
                    result.IsValid = false;

                    result.Status = "FAILED";

                    return result;
                }


                using var document =
                    JsonDocument.Parse(json);


                var root =
                    document.RootElement;


                result.Status =
                    GetString(
                        root,
                        "status");


                result.TransactionId =
                    GetString(
                        root,
                        "tran_id");


                result.ValidationId =
                    GetString(
                        root,
                        "val_id");


                result.BankTransactionId =
                    GetString(
                        root,
                        "bank_tran_id");


                result.CardType =
                    GetString(
                        root,
                        "card_type");


                result.CardIssuer =
                    GetString(
                        root,
                        "card_issuer");


                result.CardBrand =
                    GetString(
                        root,
                        "card_brand");


                result.Currency =
                    GetString(
                        root,
                        "currency");


                result.RiskTitle =
                    GetString(
                        root,
                        "risk_title");


                result.Amount =
                    GetDecimal(
                        root,
                        "amount");


                result.StoreAmount =
                    GetDecimal(
                        root,
                        "store_amount");


                result.RiskLevel =
                    GetInt(
                        root,
                        "risk_level");


                result.IsValid =
                    result.Status.Equals(
                        "VALID",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    result.Status.Equals(
                        "VALIDATED",
                        StringComparison.OrdinalIgnoreCase);


                return result;
            }
            catch
            {
                result.IsValid = false;

                result.Status = "FAILED";

                return result;
            }
        }


        // =====================================================
        // REAL SSLCOMMERZ REFUND
        // =====================================================
        public async Task<SslCommerzRefundResult>
            InitiateRefundAsync(
                string bankTransactionId,
                string refundTransactionId,
                decimal refundAmount,
                string refundRemarks,
                string referenceId)
        {
            var result =
                new SslCommerzRefundResult();


            try
            {
                // ---------------------------------------------
                // BASIC VALIDATION
                // ---------------------------------------------
                if (string.IsNullOrWhiteSpace(
                        bankTransactionId))
                {
                    result.IsSuccess = false;

                    result.Status = "failed";

                    result.ErrorReason =
                        "Bank transaction ID is missing.";

                    return result;
                }


                if (string.IsNullOrWhiteSpace(
                        refundTransactionId))
                {
                    result.IsSuccess = false;

                    result.Status = "failed";

                    result.ErrorReason =
                        "Refund transaction ID is missing.";

                    return result;
                }


                if (refundAmount <= 0)
                {
                    result.IsSuccess = false;

                    result.Status = "failed";

                    result.ErrorReason =
                        "Refund amount must be greater than zero.";

                    return result;
                }


                string storeId =
                    GetConfig("StoreId");


                string storePassword =
                    GetConfig("StorePassword");


                if (string.IsNullOrWhiteSpace(storeId) ||
                    string.IsNullOrWhiteSpace(storePassword))
                {
                    result.IsSuccess = false;

                    result.Status = "failed";

                    result.ErrorReason =
                        "SSLCommerz merchant credentials are missing.";

                    return result;
                }


                // SSLCommerz refund_trans_id maximum length
                // should remain within the gateway limit.
                refundTransactionId =
                    LimitLength(
                        refundTransactionId,
                        30);


                refundRemarks =
                    LimitLength(
                        Safe(
                            refundRemarks,
                            "PetHope order refund"),
                        255);


                referenceId =
                    LimitLength(
                        Safe(
                            referenceId,
                            refundTransactionId),
                        50);


                string refundUrl =
                    GetRefundUrl();


                string requestUrl =
                    refundUrl

                    + "?bank_tran_id="
                    + Uri.EscapeDataString(
                        bankTransactionId)

                    + "&refund_trans_id="
                    + Uri.EscapeDataString(
                        refundTransactionId)

                    + "&refund_amount="
                    + Uri.EscapeDataString(
                        refundAmount.ToString(
                            "0.00",
                            CultureInfo.InvariantCulture))

                    + "&refund_remarks="
                    + Uri.EscapeDataString(
                        refundRemarks)

                    + "&refe_id="
                    + Uri.EscapeDataString(
                        referenceId)

                    + "&store_id="
                    + Uri.EscapeDataString(
                        storeId)

                    + "&store_passwd="
                    + Uri.EscapeDataString(
                        storePassword)

                    + "&v=1"

                    + "&format=json";


                var client =
                    _httpClientFactory.CreateClient();


                var response =
                    await client.GetAsync(
                        requestUrl);


                string json =
                    await response.Content
                        .ReadAsStringAsync();


                if (!response.IsSuccessStatusCode)
                {
                    result.IsSuccess = false;

                    result.ApiConnect = "FAILED";

                    result.Status = "failed";

                    result.ErrorReason =
                        "SSLCommerz refund API HTTP request failed. "
                        +
                        "HTTP Status: "
                        +
                        (int)response.StatusCode;

                    return result;
                }


                if (string.IsNullOrWhiteSpace(json))
                {
                    result.IsSuccess = false;

                    result.ApiConnect = "FAILED";

                    result.Status = "failed";

                    result.ErrorReason =
                        "SSLCommerz returned an empty refund response.";

                    return result;
                }


                using var document =
                    JsonDocument.Parse(json);


                var root =
                    document.RootElement;


                result.ApiConnect =
                    GetString(
                        root,
                        "APIConnect");


                result.Status =
                    GetString(
                        root,
                        "status");


                result.BankTransactionId =
                    GetString(
                        root,
                        "bank_tran_id");


                result.TransactionId =
                    GetString(
                        root,
                        "trans_id");


                if (string.IsNullOrWhiteSpace(
                        result.TransactionId))
                {
                    result.TransactionId =
                        GetString(
                            root,
                            "tran_id");
                }


                result.RefundReferenceId =
                    GetString(
                        root,
                        "refund_ref_id");


                result.ErrorReason =
                    GetString(
                        root,
                        "errorReason");


                bool apiConnected =
                    result.ApiConnect.Equals(
                        "DONE",
                        StringComparison.OrdinalIgnoreCase);


                bool acceptedStatus =
                    result.Status.Equals(
                        "success",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    result.Status.Equals(
                        "processing",
                        StringComparison.OrdinalIgnoreCase);


                result.IsSuccess =
                    apiConnected
                    &&
                    acceptedStatus
                    &&
                    !string.IsNullOrWhiteSpace(
                        result.RefundReferenceId);


                if (!result.IsSuccess &&
                    string.IsNullOrWhiteSpace(
                        result.ErrorReason))
                {
                    result.ErrorReason =
                        "SSLCommerz did not accept the refund request.";
                }


                return result;
            }
            catch (JsonException ex)
            {
                result.IsSuccess = false;

                result.Status = "failed";

                result.ErrorReason =
                    "Invalid SSLCommerz refund response: "
                    + ex.Message;

                return result;
            }
            catch (HttpRequestException ex)
            {
                result.IsSuccess = false;

                result.Status = "failed";

                result.ErrorReason =
                    "Could not connect to SSLCommerz refund API: "
                    + ex.Message;

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;

                result.Status = "failed";

                result.ErrorReason =
                    "Refund processing error: "
                    + ex.Message;

                return result;
            }
        }


        // =====================================================
        // CHECK REAL REFUND STATUS FROM SSLCOMMERZ
        // =====================================================
        public async Task<SslCommerzRefundStatusResult>
            QueryRefundStatusAsync(
                string refundReferenceId)
        {
            var result =
                new SslCommerzRefundStatusResult();


            try
            {
                if (string.IsNullOrWhiteSpace(
                        refundReferenceId))
                {
                    result.IsSuccess = false;

                    result.Status = "failed";

                    result.ErrorReason =
                        "Refund reference ID is missing.";

                    return result;
                }


                string storeId =
                    GetConfig("StoreId");


                string storePassword =
                    GetConfig("StorePassword");


                if (string.IsNullOrWhiteSpace(storeId) ||
                    string.IsNullOrWhiteSpace(storePassword))
                {
                    result.IsSuccess = false;

                    result.Status = "failed";

                    result.ErrorReason =
                        "SSLCommerz merchant credentials are missing.";

                    return result;
                }


                string refundUrl =
                    GetRefundUrl();


                string requestUrl =
                    refundUrl

                    + "?refund_ref_id="
                    + Uri.EscapeDataString(
                        refundReferenceId)

                    + "&store_id="
                    + Uri.EscapeDataString(
                        storeId)

                    + "&store_passwd="
                    + Uri.EscapeDataString(
                        storePassword)

                    + "&format=json";


                var client =
                    _httpClientFactory.CreateClient();


                var response =
                    await client.GetAsync(
                        requestUrl);


                string json =
                    await response.Content
                        .ReadAsStringAsync();


                if (!response.IsSuccessStatusCode)
                {
                    result.IsSuccess = false;

                    result.ApiConnect = "FAILED";

                    result.Status = "failed";

                    result.ErrorReason =
                        "SSLCommerz refund status API HTTP request failed. "
                        +
                        "HTTP Status: "
                        +
                        (int)response.StatusCode;

                    return result;
                }


                if (string.IsNullOrWhiteSpace(json))
                {
                    result.IsSuccess = false;

                    result.ApiConnect = "FAILED";

                    result.Status = "failed";

                    result.ErrorReason =
                        "SSLCommerz returned an empty refund status response.";

                    return result;
                }


                using var document =
                    JsonDocument.Parse(json);


                var root =
                    document.RootElement;


                result.ApiConnect =
                    GetString(
                        root,
                        "APIConnect");


                result.Status =
                    GetString(
                        root,
                        "status");


                result.BankTransactionId =
                    GetString(
                        root,
                        "bank_tran_id");


                result.TransactionId =
                    GetString(
                        root,
                        "trans_id");


                if (string.IsNullOrWhiteSpace(
                        result.TransactionId))
                {
                    result.TransactionId =
                        GetString(
                            root,
                            "tran_id");
                }


                result.RefundReferenceId =
                    GetString(
                        root,
                        "refund_ref_id");


                result.ErrorReason =
                    GetString(
                        root,
                        "errorReason");


                result.InitiatedOn =
                    GetNullableDateTime(
                        root,
                        "initiated_on");


                result.RefundedOn =
                    GetNullableDateTime(
                        root,
                        "refunded_on");


                result.IsSuccess =
                    result.ApiConnect.Equals(
                        "DONE",
                        StringComparison.OrdinalIgnoreCase);


                result.IsRefunded =
                    result.Status.Equals(
                        "refunded",
                        StringComparison.OrdinalIgnoreCase);


                result.IsProcessing =
                    result.Status.Equals(
                        "processing",
                        StringComparison.OrdinalIgnoreCase);


                result.IsCancelled =
                    result.Status.Equals(
                        "cancelled",
                        StringComparison.OrdinalIgnoreCase);


                if (!result.IsSuccess &&
                    string.IsNullOrWhiteSpace(
                        result.ErrorReason))
                {
                    result.ErrorReason =
                        "Could not retrieve refund status from SSLCommerz.";
                }


                return result;
            }
            catch (JsonException ex)
            {
                result.IsSuccess = false;

                result.Status = "failed";

                result.ErrorReason =
                    "Invalid SSLCommerz refund status response: "
                    + ex.Message;

                return result;
            }
            catch (HttpRequestException ex)
            {
                result.IsSuccess = false;

                result.Status = "failed";

                result.ErrorReason =
                    "Could not connect to SSLCommerz refund status API: "
                    + ex.Message;

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;

                result.Status = "failed";

                result.ErrorReason =
                    "Refund status check error: "
                    + ex.Message;

                return result;
            }
        }


        // =====================================================
        // CONFIGURATION
        // =====================================================
        private string GetConfig(string key)
        {
            return
                _configuration[
                    $"SslCommerz:{key}"]
                ?? string.Empty;
        }


        private bool IsSandbox()
        {
            string? value =
                _configuration[
                    "SslCommerz:IsSandbox"];


            if (bool.TryParse(
                value,
                out bool isSandbox))
            {
                return isSandbox;
            }


            return true;
        }


        // =====================================================
        // PAYMENT SESSION URL
        // =====================================================
        private string GetSessionUrl()
        {
            if (IsSandbox())
            {
                return
                    GetConfig(
                        "SandboxSessionUrl");
            }


            return
                GetConfig(
                    "LiveSessionUrl");
        }


        // =====================================================
        // PAYMENT VALIDATION URL
        // =====================================================
        private string GetValidationUrl()
        {
            if (IsSandbox())
            {
                return
                    GetConfig(
                        "SandboxValidationUrl");
            }


            return
                GetConfig(
                    "LiveValidationUrl");
        }


        // =====================================================
        // REAL REFUND API URL
        // =====================================================
        private string GetRefundUrl()
        {
            if (IsSandbox())
            {
                return
                    "https://sandbox.sslcommerz.com/validator/api/merchantTransIDvalidationAPI.php";
            }


            return
                "https://securepay.sslcommerz.com/validator/api/merchantTransIDvalidationAPI.php";
        }


        // =====================================================
        // STRING SAFETY
        // =====================================================
        private string Safe(
            string? value,
            string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }


            return value.Trim();
        }


        // =====================================================
        // LIMIT STRING LENGTH
        // =====================================================
        private string LimitLength(
            string? value,
            int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }


            string trimmed =
                value.Trim();


            if (trimmed.Length <= maxLength)
            {
                return trimmed;
            }


            return trimmed.Substring(
                0,
                maxLength);
        }


        // =====================================================
        // READ JSON STRING
        // =====================================================
        private string GetString(
            JsonElement root,
            string propertyName)
        {
            if (root.TryGetProperty(
                propertyName,
                out JsonElement value))
            {
                if (value.ValueKind ==
                    JsonValueKind.Null)
                {
                    return string.Empty;
                }


                return value.ToString();
            }


            return string.Empty;
        }


        // =====================================================
        // READ JSON DECIMAL
        // =====================================================
        private decimal GetDecimal(
            JsonElement root,
            string propertyName)
        {
            string text =
                GetString(
                    root,
                    propertyName);


            if (decimal.TryParse(
                text,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out decimal number))
            {
                return number;
            }


            if (decimal.TryParse(
                text,
                out number))
            {
                return number;
            }


            return 0;
        }


        // =====================================================
        // READ JSON INTEGER
        // =====================================================
        private int GetInt(
            JsonElement root,
            string propertyName)
        {
            string text =
                GetString(
                    root,
                    propertyName);


            if (int.TryParse(
                text,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out int number))
            {
                return number;
            }


            return 0;
        }


        // =====================================================
        // READ SSLCOMMERZ DATE
        // =====================================================
        private DateTime? GetNullableDateTime(
            JsonElement root,
            string propertyName)
        {
            string text =
                GetString(
                    root,
                    propertyName);


            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }


            string[] formats =
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd H:mm:ss",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ssZ"
            };


            if (DateTime.TryParseExact(
                text,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out DateTime exactDate))
            {
                return exactDate;
            }


            if (DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out DateTime date))
            {
                return date;
            }


            return null;
        }
    }
}