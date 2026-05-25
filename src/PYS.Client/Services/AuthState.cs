using PYS.Client.Models;

namespace PYS.Client.Services;

public sealed class AuthState
{
    public AuthResponse? Current { get; private set; }

    public bool IsAuthenticated => Current is not null && Current.ExpiresAt > DateTime.UtcNow;

    public event EventHandler<AuthResponse?>? Changed;

    public void Set(AuthResponse response)
    {
        Current = response;
        Changed?.Invoke(this, response);
    }

    public void Clear()
    {
        Current = null;
        Changed?.Invoke(this, null);
    }
}
