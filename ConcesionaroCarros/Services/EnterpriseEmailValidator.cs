using System;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Web.Script.Serialization;

namespace ConcesionaroCarros.Services
{
    public class EnterpriseEmailValidationResult
    {
        public bool IsValid { get; set; }
        public bool ValidatedByMicrosoft { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class EnterpriseEmailValidator
    {
        private readonly string _requiredDomain;
        private readonly bool _requireMicrosoftValidation;

        public EnterpriseEmailValidator()
        {
            _requiredDomain = (GetSetting("CC_CORPORATE_EMAIL_DOMAIN", "weg.net") ?? "weg.net")
                .Trim()
                .TrimStart('@')
                .ToLowerInvariant();

            _requireMicrosoftValidation = ReadBoolEnv(
                "CC_REQUIRE_MICROSOFT_EMAIL_VALIDATION",
                defaultValue: true);
        }

        public EnterpriseEmailValidationResult ValidateCorporateEmail(string correo)
        {
            var result = new EnterpriseEmailValidationResult();
            var correoSeguro = (correo ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(correoSeguro))
            {
                result.IsValid = false;
                result.ErrorMessage = "El correo es obligatorio para recuperar la contrasena.";
                return result;
            }

            if (!IsEmailFormatValid(correoSeguro))
            {
                result.IsValid = false;
                result.ErrorMessage = "El formato del correo no es valido.";
                return result;
            }

            if (!correoSeguro.EndsWith("@" + _requiredDomain, StringComparison.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.ErrorMessage = "El correo debe pertenecer al dominio corporativo @" + _requiredDomain + ".";
                return result;
            }

            if (!_requireMicrosoftValidation)
            {
                result.IsValid = true;
                result.ValidatedByMicrosoft = false;
                return result;
            }

            string errorMicrosoft;
            var validadoMicrosoft = ValidateWithMicrosoftGraph(correoSeguro, out errorMicrosoft);
            if (!validadoMicrosoft)
            {
                result.IsValid = false;
                result.ErrorMessage = errorMicrosoft;
                result.ValidatedByMicrosoft = false;
                return result;
            }

            result.IsValid = true;
            result.ValidatedByMicrosoft = true;
            return result;
        }

        private static bool ReadBoolEnv(string key, bool defaultValue)
        {
            var raw = GetSetting(key, null);
            if (string.IsNullOrWhiteSpace(raw))
                return defaultValue;

            raw = raw.Trim();
            return string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(raw, "si", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSetting(string key, string defaultValue)
        {
            try
            {
                var configValue = ConfigurationManager.AppSettings[key];
                if (!string.IsNullOrWhiteSpace(configValue))
                    return configValue.Trim();
            }
            catch
            {
                // Si falla App.config, continuamos con variables de entorno.
            }

            var envValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(envValue))
                return envValue.Trim();

            return defaultValue;
        }

        private static bool IsEmailFormatValid(string correo)
        {
            try
            {
                var addr = new MailAddress(correo);
                return string.Equals(addr.Address, correo, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool ValidateWithMicrosoftGraph(string correo, out string error)
        {
            error = string.Empty;

            var tenantId = (GetSetting("CC_AZURE_TENANT_ID", string.Empty) ?? string.Empty).Trim();
            var clientId = (GetSetting("CC_AZURE_CLIENT_ID", string.Empty) ?? string.Empty).Trim();
            var clientSecret = (GetSetting("CC_AZURE_CLIENT_SECRET", string.Empty) ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(tenantId) ||
                string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(clientSecret))
            {
                error = "Falta configuracion de Microsoft Graph. Defina CC_AZURE_TENANT_ID, CC_AZURE_CLIENT_ID y CC_AZURE_CLIENT_SECRET.";
                return false;
            }

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(12);

                    var tokenEndpoint = "https://login.microsoftonline.com/" + tenantId + "/oauth2/v2.0/token";
                    var tokenBody = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("client_id", clientId),
                        new KeyValuePair<string, string>("client_secret", clientSecret),
                        new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
                        new KeyValuePair<string, string>("grant_type", "client_credentials")
                    });

                    var tokenResponse = http.PostAsync(tokenEndpoint, tokenBody).Result;
                    if (!tokenResponse.IsSuccessStatusCode)
                    {
                        error = "No fue posible autenticarse en Microsoft Graph. Codigo: " + (int)tokenResponse.StatusCode + ".";
                        return false;
                    }

                    var tokenJson = tokenResponse.Content.ReadAsStringAsync().Result;
                    var serializer = new JavaScriptSerializer();
                    var tokenData = serializer.Deserialize<Dictionary<string, object>>(tokenJson);

                    string accessToken;
                    if (tokenData == null ||
                        !tokenData.TryGetValue("access_token", out var accessTokenObj) ||
                        string.IsNullOrWhiteSpace(accessToken = Convert.ToString(accessTokenObj)))
                    {
                        error = "Microsoft Graph no devolvio un access_token valido.";
                        return false;
                    }

                    var userEndpoint = "https://graph.microsoft.com/v1.0/users/" +
                                       Uri.EscapeDataString(correo) +
                                       "?$select=id,userPrincipalName,mail";

                    using (var request = new HttpRequestMessage(HttpMethod.Get, userEndpoint))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        var userResponse = http.SendAsync(request).Result;

                        if (userResponse.StatusCode == HttpStatusCode.NotFound)
                        {
                            error = "El correo no existe en Microsoft Entra ID.";
                            return false;
                        }

                        if (!userResponse.IsSuccessStatusCode)
                        {
                            error = "Microsoft Graph rechazo la validacion del correo. Codigo: " +
                                    (int)userResponse.StatusCode + ".";
                            return false;
                        }

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                error = "No fue posible validar el correo en Microsoft. " + ex.Message;
                return false;
            }
        }
    }
}
