using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace AutoSortFix.Patches
{
    internal static class SortGuard
    {
        public static ManualLogSource LogSource;
        public static bool Suppressing;

        public static void FinishSort()
        {
            if (!Suppressing)
                return;
            Suppressing = false;
            Reconcile();
        }

        private static void Reconcile()
        {
            var grids = UnityEngine.Object.FindObjectsOfType<GridView>();
            int totalContained = 0;
            var perGrid = new List<Tuple<GridView, Dictionary<Item, LocationInGrid>>>();
            foreach (var gridView in grids)
            {
                try
                {
                    if (gridView == null || gridView.Grid == null)
                        continue;
                    var wanted = new Dictionary<Item, LocationInGrid>();
                    foreach (KeyValuePair<Item, LocationInGrid> kvp in gridView.Grid.ContainedItems)
                    {
                        if (kvp.Key != null && !wanted.ContainsKey(kvp.Key))
                            wanted.Add(kvp.Key, kvp.Value);
                    }
                    totalContained += wanted.Count;
                    perGrid.Add(Tuple.Create(gridView, wanted));
                }
                catch (Exception e)
                {
                    LogSource?.LogWarning($"SortFix: grid scan failed: {e.Message}");
                }
            }

            // Never wipe views when the model looks unsettled (e.g. empty everywhere).
            if (totalContained == 0)
            {
                LogSource?.LogWarning("SortFix: reconcile skipped, no contained items found.");
                return;
            }

            int kept = 0, killedStale = 0, gridsTouched = 0;
            foreach (var entry in perGrid)
            {
                var gridView = entry.Item1;
                var wanted = entry.Item2;
                try
                {
                    foreach (var view in gridView.GridItemViews.ToList())
                    {
                        try
                        {
                            if (view == null || view.Item == null || !wanted.TryGetValue(view.Item, out var loc))
                            {
                                view?.Kill();
                                killedStale++;
                            }
                            else
                            {
                                view.IsBeingAdded.Value = false;
                                view.IsBeingRemoved.Value = false;
                                view.IsBeingDrained.Value = false;
                                gridView.SetItemViewPosition(view, loc);
                                kept++;
                            }
                        }
                        catch (Exception e)
                        {
                            LogSource?.LogWarning($"SortFix: view fixup failed: {e.Message}");
                        }
                    }

                    bool missing = false;
                    foreach (var item in wanted.Keys)
                    {
                        if (gridView.FindItemView(item) == null)
                        {
                            missing = true;
                            break;
                        }
                    }
                    if (missing)
                        gridView.PrepareItems();
                    gridView.ManageActiveEvents();
                    gridsTouched++;
                }
                catch (Exception e)
                {
                    LogSource?.LogError($"SortFix: reconcile failed: {e}");
                }
            }
            LogSource?.LogInfo($"SortFix: reconciled {gridsTouched} grids, kept {kept}, killed stale {killedStale}.");
        }
    }

    internal sealed class SortArmPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(GridSortPanel), nameof(GridSortPanel.Sort));

        [PatchPrefix]
        private static void Prefix()
        {
            if (AutoSortFix.Settings.Enabled.Value)
            {
                SortGuard.Suppressing = true;
                SortGuard.LogSource?.LogInfo("SortFix: sort started, grid events suppressed.");
            }
        }
    }

    internal sealed class SortDonePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(GridSortPanel), nameof(GridSortPanel.ChangeProgress));

        [PatchPostfix]
        private static void Postfix(bool inProgress)
        {
            if (!inProgress && AutoSortFix.Settings.Enabled.Value)
                SortGuard.FinishSort();
        }
    }

    internal sealed class SuppressAddPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            PatchHelpers.FindExplicitHandler(typeof(GridView), "OnItemAdded", typeof(AddItemEventArgs), "EFT.UI.DragAndDrop.IAddHandler");

        [PatchPrefix]
        private static bool Prefix() => !ShouldSuppress();

        private static bool ShouldSuppress() =>
            AutoSortFix.Settings.Enabled.Value && SortGuard.Suppressing;
    }

    internal sealed class SuppressRemovePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            PatchHelpers.FindExplicitHandler(typeof(GridView), "OnItemRemoved", typeof(RemoveItemEventArgs), "EFT.UI.DragAndDrop.IRemoveHandler");

        [PatchPrefix]
        private static bool Prefix() => !ShouldSuppress();

        private static bool ShouldSuppress() =>
            AutoSortFix.Settings.Enabled.Value && SortGuard.Suppressing;
    }

    internal static class PatchHelpers
    {
        // Explicit interface impls are named "Namespace.IInterface.Method" in metadata.
        public static MethodBase FindExplicitHandler(Type type, string shortName, Type argType, string iface)
        {
            return AccessTools.Method(type, $"{iface}.{shortName}")
                ?? type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name.EndsWith(shortName, StringComparison.Ordinal)
                        && m.GetParameters() is var p && p.Length == 1 && p[0].ParameterType == argType);
        }
    }
}
