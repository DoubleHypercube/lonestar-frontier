using Content.Client.Eui;
using Content.Shared.CrewManifest;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.CrewManifest;

[UsedImplicitly]
public sealed class CrewManifestEui : BaseEui
{
    [Dependency] private readonly SharedCrewManifestSystem _manifest = default!;

    private readonly CrewManifestUi _window;

    public CrewManifestEui()
    {
        IoCManager.InjectDependencies(this);

        _window = new();

        _window.OnClose += () =>
        {
            SendMessage(new CloseEuiMessage());
        };
    }

    public override void Opened()
    {
        base.Opened();

        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not CrewManifestEuiState cast)
        {
            Logger.Info("State is invalid?!");
            return;
        }

        if (cast.Entries == null) Logger.Info($"We are rebuilding entries");
        var entries = cast.Entries ?? _manifest.BuildCrewManifest();

        Logger.Info($"We have {entries.Entries.Length} entries to handle");
        foreach (var entry in entries.Entries)
        {
            Logger.Info($"{entry.Name} is a {entry.JobTitle} ({entry.JobPrototype} with icon {entry.JobIcon})");
        }

        _window.Populate(entries); // Coyote: Remove name
    }
}
