using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class AuthService
{
    private const string Endpoint =
        "https://api-portalweb-respaldo.bsginstitute.com/api/AspNetUser/authenticate";

    [Serializable]
    private class LoginRequest
    {
        public string Username;
        public string Password;
    }

    [Serializable]
    private class ExcepcionDto
    {
        public bool excepcionGenerada;
        public string descripcionGeneral;
        public string error;
    }

    [Serializable]
    private class AuthResponseDto
    {
        public string userName;
        public string token;
        public ExcepcionDto excepcion;
    }

    public struct AuthResult
    {
        public bool Success;
        public long StatusCode;
        public string Token;
        public string UserName;
        public string ErrorMessage;
        public string RawBody;
    }

    public static async Task<AuthResult> AuthenticateAsync(string email, string password)
    {
        // 1) Armamos el JSON que el backend espera
        var payload = new LoginRequest
        {
            Username = email,
            Password = password
        };

        var json = JsonUtility.ToJson(payload);
        var body = Encoding.UTF8.GetBytes(json);

        // 2) Creamos request POST con body JSON
        using var req = new UnityWebRequest(Endpoint, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        // 3) Enviamos async (sin congelar la app)
        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        var result = new AuthResult
        {
            StatusCode = req.responseCode,
            RawBody = req.downloadHandler?.text
        };

        // 4) Si es problema de internet / conexión / TLS
        if (req.result == UnityWebRequest.Result.ConnectionError ||
            req.result == UnityWebRequest.Result.DataProcessingError)
        {
            result.Success = false;
            result.ErrorMessage = req.error;
            return result;
        }

        // 5) Parseamos respuesta JSON (si existe)
        AuthResponseDto dto = null;
        if (!string.IsNullOrEmpty(result.RawBody))
        {
            try { dto = JsonUtility.FromJson<AuthResponseDto>(result.RawBody); }
            catch { /* si falla, igual devolvemos RawBody para debug */ }
        }

        // 6) Si HTTP 2xx y hay token => login OK
        if (req.responseCode >= 200 && req.responseCode < 300 &&
            dto != null && !string.IsNullOrEmpty(dto.token))
        {
            result.Success = true;
            result.Token = dto.token;
            result.UserName = dto.userName;
            return result;
        }

        // 7) Si no fue OK, sacamos el mensaje del backend (si vino)
        result.Success = false;
        result.ErrorMessage = dto?.excepcion?.descripcionGeneral;

        if (string.IsNullOrEmpty(result.ErrorMessage))
            result.ErrorMessage = $"HTTP {req.responseCode}";

        return result;
    }
}
