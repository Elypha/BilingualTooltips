using BilingualTooltips.Addons;
using Dalamud.Game.Addon.Lifecycle;

namespace BilingualTooltips.Modules;

public partial class TooltipHandler
{
    private BilingualTooltipsPlugin plugin;
    public readonly ItemDetailAddon itemDetailAddon;
    public readonly ActionDetailAddon actionDetailAddon;
    public TooltipHandler(BilingualTooltipsPlugin plugin)
    {
        this.plugin = plugin;
        itemDetailAddon = new ItemDetailAddon(plugin);
        actionDetailAddon = new ActionDetailAddon(plugin);
    }
    public void Dispose()
    {
        StopHook();
        itemDetailAddon.Dispose();
        actionDetailAddon.Dispose();
    }


    public void StartHook()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "ItemDetail", itemDetailAddon.PreRequestedUpdateHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "ActionDetail", actionDetailAddon.PreRequestedUpdateHandler);
    }

    public void StopHook()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "ItemDetail", itemDetailAddon.PreRequestedUpdateHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "ActionDetail", actionDetailAddon.PreRequestedUpdateHandler);
    }
}
