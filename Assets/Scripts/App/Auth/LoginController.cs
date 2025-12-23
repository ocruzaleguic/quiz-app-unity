using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LoginController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private TextField _emailInput;
    private TextField _passwordInput;
    private Button _primaryBtn;
    private Label _loginError;

    private static readonly Regex EmailRegex =
        new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

        var root = uiDocument.rootVisualElement;

        // Tus names reales (según lo que vienes usando)
        _emailInput = root.Q<TextField>("emailInput");
        _passwordInput = root.Q<TextField>("passwordInput");
        _primaryBtn = root.Q<Button>("primaryBtn");

        // El label que agregaste encima del botón
        _loginError = root.Q<Label>("loginError");

        _primaryBtn.clicked += OnLoginClicked;

        // Si el usuario vuelve a escribir, ocultamos el error
        _emailInput.RegisterValueChangedCallback(_ => HideError());
        _passwordInput.RegisterValueChangedCallback(_ => HideError());
    }

    private async void OnLoginClicked()
    {
        HideError();

        var email = _emailInput.value?.Trim() ?? "";
        var pass = _passwordInput.value ?? "";

        // Validación básica local (antes de ir al backend)
        if (string.IsNullOrEmpty(email) || !EmailRegex.IsMatch(email))
        {
            ShowError("Escribe un correo válido.");
            return;
        }

        if (string.IsNullOrEmpty(pass))
        {
            ShowError("Escribe tu contraseña.");
            return;
        }

        SetBusy(true);

        var res = await AuthService.AuthenticateAsync(email, pass);

        SetBusy(false);

        if (res.Success)
        {
            AuthSession.Set(res.Token, res.UserName);
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        // Endpoint no diferencia correo/clave => mensaje genérico
        if (res.StatusCode == 401)
        {
            ShowError("Correo o contraseña incorrectos.");
            return;
        }

        // Otros errores (server caído, 500, etc.)
        ShowError("No se pudo iniciar sesión. Intenta de nuevo.");
        Debug.LogWarning($"Login failed: {res.StatusCode} body={res.RawBody}");
    }

    private void SetBusy(bool busy)
    {
        _primaryBtn.SetEnabled(!busy);
        _emailInput.SetEnabled(!busy);
        _passwordInput.SetEnabled(!busy);

        // Opcional: cambia texto del botón mientras carga
        // (si tu botón tiene label interno, puedes buscarlo y cambiarlo)
    }

    private void ShowError(string msg)
    {
        if (_loginError == null) return;
        _loginError.text = msg;
        _loginError.style.display = DisplayStyle.Flex;
    }

    private void HideError()
    {
        if (_loginError == null) return;
        _loginError.text = "";
        _loginError.style.display = DisplayStyle.None;
    }
}
