using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BeefChowFun.Workers;

internal unsafe class AutoRetainerPricer : WorkerBase
{
    public override string Name => "自动雇员改价";
    public override bool IsBusy => _taskManager?.IsBusy ?? false;

    private TaskManager? _taskManager;
    private readonly Dictionary<string, (uint Price, DateTime Time)> _priceCache = new();
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(5);

    public override void Init()
    {
        _taskManager = new TaskManager();
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", OnRetainerSellOpen);
        Log("自动雇员改价 Worker 已初始化");
    }

    public override void Uninit()
    {
        _taskManager?.Abort();
        _taskManager = null;
        Svc.AddonLifecycle.UnregisterListener(OnRetainerSellOpen);
        _priceCache.Clear();
    }

    public override void DrawConfig()
    {
        ImGui.TextWrapped("自动雇员改价功能 - 从市场板获取价格并调整");
        ImGui.Separator();
        
        var enabled = Config.PriceAdjustWorkerEnabled;
        if (ImGui.Checkbox("启用自动改价", ref enabled))
        {
            Config.PriceAdjustWorkerEnabled = enabled;
            BeefChowFun.Instance.SaveConfig();
        }

        if (!Config.PriceAdjustWorkerEnabled) ImGui.BeginDisabled();

        ImGui.Spacing();
        var strategies = new[] { "压价1金", "减固定金额", "固定价格" };
        var strat = Config.PriceAdjustStrategy;
        ImGuiHelper.Text("改价策略:"); ImGui.SameLine(); ImGui.SetNextItemWidth(150);
        if (ImGui.Combo("##Strategy", ref strat, strategies, strategies.Length))
        {
            Config.PriceAdjustStrategy = strat;
            BeefChowFun.Instance.SaveConfig();
        }

        if (strat == 1)
        {
            ImGui.SameLine(); ImGuiHelper.Text("减少:"); ImGui.SameLine();
            var r = Config.PriceReductionAmount; ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt("##Red", ref r))
            {
                Config.PriceReductionAmount = Math.Max(1, r);
                BeefChowFun.Instance.SaveConfig();
            }
        }
        else if (strat == 2)
        {
            ImGui.SameLine(); ImGuiHelper.Text("价格:"); ImGui.SameLine();
            var fp = Config.FixedPrice; ImGui.SetNextItemWidth(120);
            if (ImGui.InputInt("##Fix", ref fp))
            {
                Config.FixedPrice = Math.Max(1, fp);
                BeefChowFun.Instance.SaveConfig();
            }
        }

        ImGui.Spacing();
        var minP = Config.MinimumPrice;
        ImGuiHelper.Text("最低价:"); ImGui.SameLine(); ImGui.SetNextItemWidth(120);
        if (ImGui.InputInt("##Min", ref minP))
        {
            Config.MinimumPrice = Math.Max(1, minP);
            BeefChowFun.Instance.SaveConfig();
        }

        var maxP = Config.MaximumPrice;
        ImGuiHelper.Text("最高价:"); ImGui.SameLine(); ImGui.SetNextItemWidth(120);
        if (ImGui.InputInt("##Max", ref maxP))
        {
            Config.MaximumPrice = Math.Max(Config.MinimumPrice + 1, maxP);
            BeefChowFun.Instance.SaveConfig();
        }

        ImGui.Spacing(); ImGui.Separator();
        if (IsBusy) ImGui.BeginDisabled();
        if (ImGui.Button("批量改价所有雇员")) EnqueueAllRetainers();
        if (IsBusy)
        {
            ImGui.EndDisabled(); ImGui.SameLine();
            if (ImGui.Button("停止")) _taskManager?.Abort();
            ImGui.SameLine();
            ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "改价中...");
        }
        if (!Config.PriceAdjustWorkerEnabled) ImGui.EndDisabled();
    }

    private void OnRetainerSellOpen(AddonEvent type, AddonArgs args)
    {
        if (!Config.PriceAdjustWorkerEnabled) return;
        _taskManager?.EnqueueDelay(500);
        _taskManager?.Enqueue(AdjustCurrentRetainerPrices);
    }

    private bool? AdjustCurrentRetainerPrices()
    {
        if (!EzThrottler.Throttle("AdjustPrices", Config.OperationDelay)) return false;
        var inv = InventoryManager.Instance();
        if (inv == null) return true;
        var mc = inv->GetInventoryContainer(InventoryType.RetainerMarket);
        if (mc == null || !mc->IsLoaded) return false;

        ExpireCache();
        for (var i = 0; i < mc->Size; i++)
        {
            var slot = mc->GetInventorySlot(i);
            if (slot == null || slot->ItemId == 0) continue;
            
            // 使用 InventoryManager 的方法获取当前价格
            var currentPrice = (uint)inv->GetRetainerMarketPrice((short)i);
            if (currentPrice == 0) continue;
            
            var newPrice = CalculatePrice(slot);
            if (newPrice == 0 || newPrice == currentPrice) continue;
            var idx = (ushort)i; var p = newPrice;
            _taskManager?.Enqueue(() => SetSlotPrice(idx, p));
            _taskManager?.EnqueueDelay(Config.OperationDelay);
        }
        return true;
    }

    private uint CalculatePrice(InventoryItem* slot)
    {
        if (slot == null) return 0;
        var itemId = slot->ItemId;
        var isHq = slot->Flags.HasFlag(InventoryItem.ItemFlags.HighQuality);
        var marketPrice = GetCachedPrice(itemId, isHq);
        if (marketPrice == 0)
        {
            marketPrice = FetchMarketPrice(itemId, isHq);
            if (marketPrice > 0) CachePrice(itemId, isHq, marketPrice);
        }
        var basePrice = marketPrice > 0 ? marketPrice : (uint)InventoryManager.Instance()->GetRetainerMarketPrice((short)slot->Slot);
        if (basePrice == 0) return 0;
        uint newPrice = Config.PriceAdjustStrategy switch
        {
            0 => basePrice > 1 ? basePrice - 1 : basePrice,
            1 => basePrice > (uint)Config.PriceReductionAmount ? basePrice - (uint)Config.PriceReductionAmount : 1u,
            2 => (uint)Config.FixedPrice,
            _ => basePrice
        };
        newPrice = Math.Max((uint)Config.MinimumPrice, newPrice);
        newPrice = Math.Min((uint)Config.MaximumPrice, newPrice);
        return newPrice;
    }

    private uint FetchMarketPrice(uint itemId, bool isHq)
    {
        var info = InfoProxyItemSearch.Instance();
        if (info == null) return 0;
        info->SearchItemId = itemId;
        info->RequestData();
        System.Threading.Thread.Sleep(100);
        var listings = info->Listings.ToArray().Where(x => x.ItemId == itemId && x.UnitPrice > 0).ToArray();
        if (listings.Length == 0) return 0;
        var filtered = isHq ? listings.Where(x => x.IsHqItem).ToArray() : listings.Where(x => !x.IsHqItem).ToArray();
        if (filtered.Length == 0) filtered = listings;
        return filtered.Min(x => x.UnitPrice);
    }

    private bool? SetSlotPrice(ushort slot, uint price)
    {
        if (!EzThrottler.Throttle($"SetPrice_{slot}", 100)) return false;
        if (price == 0) return true;
        var addonPtr = Svc.GameGui.GetAddonByName("RetainerSellList");
        if (addonPtr.IsNull) return false;
        var addon = (AtkUnitBase*)addonPtr.Address;
        if (!addon->IsVisible) return false;
        try
        {
            ECommons.Automation.Callback.Fire(addon, true, 0, (int)slot, (int)price);
            Log($"已设置槽位 {slot} 价格为 {price}");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"设置价格失败: {ex.Message}");
            return false;
        }
    }

    private void EnqueueAllRetainers()
    {
        if (_taskManager == null || IsBusy) return;
        var mgr = RetainerManager.Instance();
        if (mgr == null) return;
        var count = mgr->GetRetainerCount();
        for (uint i = 0; i < count; i++)
        {
            if (mgr->GetRetainerBySortedIndex(i) == null) continue;
            var idx = i;
            _taskManager.Enqueue(() => EnterRetainer(idx));
            _taskManager.EnqueueDelay(Config.OperationDelay);
            _taskManager.Enqueue(() => ClickSelectString(5));
            _taskManager.EnqueueDelay(Config.OperationDelay);
            _taskManager.Enqueue(AdjustCurrentRetainerPrices);
            _taskManager.EnqueueDelay(Config.OperationDelay);
            _taskManager.Enqueue(LeaveRetainer);
            _taskManager.EnqueueDelay(Config.OperationDelay);
        }
        _taskManager.Enqueue(() => { Log("批量改价完成"); return true; });
    }

    private bool? EnterRetainer(uint index)
    {
        if (!EzThrottler.Throttle($"Enter_{index}", 500)) return false;
        var mgr = RetainerManager.Instance();
        if (mgr == null) return false;
        var addonPtr = Svc.GameGui.GetAddonByName("RetainerList");
        if (addonPtr.IsNull) return false;
        var addon = (AtkUnitBase*)addonPtr.Address;
        if (!addon->IsVisible) return false;
        ECommons.Automation.Callback.Fire(addon, true, 2, (int)index, 0);
        return true;
    }

    private bool? ClickSelectString(int index)
    {
        var addonPtr = Svc.GameGui.GetAddonByName("SelectString");
        if (addonPtr.IsNull) return false;
        var addon = (AtkUnitBase*)addonPtr.Address;
        if (!addon->IsVisible) return false;
        ECommons.Automation.Callback.Fire(addon, true, index);
        return true;
    }

    private bool? LeaveRetainer()
    {
        if (!EzThrottler.Throttle("Leave", 300)) return false;
        var addonPtr = Svc.GameGui.GetAddonByName("RetainerSell");
        if (!addonPtr.IsNull)
        {
            var addon = (AtkUnitBase*)addonPtr.Address;
            if (addon->IsVisible) addon->Close(true);
        }
        addonPtr = Svc.GameGui.GetAddonByName("RetainerList");
        if (!addonPtr.IsNull)
        {
            var addon = (AtkUnitBase*)addonPtr.Address;
            if (addon->IsVisible) addon->Close(true);
        }
        return true;
    }

    private string CacheKey(uint itemId, bool isHq) => $"{itemId}_{(isHq ? "hq" : "nq")}";

    private void CachePrice(uint itemId, bool isHq, uint price)
    {
        _priceCache[CacheKey(itemId, isHq)] = (price, DateTime.UtcNow);
    }

    private uint GetCachedPrice(uint itemId, bool isHq)
    {
        var key = CacheKey(itemId, isHq);
        if (_priceCache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow - entry.Time < _cacheTTL) return entry.Price;
        }
        return 0;
    }

    private void ExpireCache()
    {
        var now = DateTime.UtcNow;
        var expired = _priceCache.Where(kv => now - kv.Value.Time > _cacheTTL).Select(kv => kv.Key).ToList();
        foreach (var key in expired) _priceCache.Remove(key);
    }
}
