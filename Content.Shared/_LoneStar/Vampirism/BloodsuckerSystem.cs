using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Lonestar.Vampirism;

public sealed class BloodsuckerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodsuckerComponent, SuckBloodEvent>(OnBite);
        SubscribeLocalEvent<BloodsuckerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BloodsuckerComponent, SuckBloodDoAfterEvent>(TryFinishDoafter);
    }

    public void OnInit(EntityUid uid, BloodsuckerComponent component, ComponentInit args) => _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);

    public void OnBite(Entity<BloodsuckerComponent> ent, ref SuckBloodEvent args)
    {
        if (args.Handled)
            return;

        _audio.PlayPredicted(ent.Comp.BiteSound, args.Performer, args.Performer);
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, 2.5F, new SuckBloodDoAfterEvent(), ent, target: args.Target));
        args.Handled = true;
    }

    private void TryFinishDoafter(Entity<BloodsuckerComponent> ent, ref SuckBloodDoAfterEvent args)
    {
        if (args.Args.Target is not { } target || args.Cancelled || args.Handled)
            return;

        args.Handled |= TryInject(ent.Comp, target, args.User);
    }

    public bool TryInject(BloodsuckerComponent component, EntityUid target, EntityUid user)
    {
        if (!TryComp<BloodstreamComponent>(target, out var bloodstream) || !TryComp<BloodstreamComponent>(user, out _))
            return false;

        if (!_solution.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var solution))
            return false;

        var reagent = solution.Contents[0].Reagent;
        var removed = solution.RemoveReagent(reagent, component.Amount);
        if (removed > 0 && _bloodstream.TryAddToChemicals(user, new Solution(reagent.Prototype, removed)))
            _audio.PlayPredicted(component.SuckSound, user, user);

        _actions.StartUseDelay(component.ActionEntity);
        return true;
    }
}
