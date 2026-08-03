namespace OpenDeviceToolkit.Hardware;

public enum ProbeRisk { Passive, Low, Moderate, Destructive }
public sealed record ProbeAction(string Name, ProbeRisk Risk, string Effect, bool RequiresConfirmation);

public sealed class ProbeAuthorization
{
    public ProbeAction Evaluate(string name, ProbeRisk risk, string effect)
        => new(name, risk, effect, risk is ProbeRisk.Moderate or ProbeRisk.Destructive);

    public bool IsAuthorized(ProbeAction action, bool operatorConfirmed)
        => !action.RequiresConfirmation || operatorConfirmed;
}
