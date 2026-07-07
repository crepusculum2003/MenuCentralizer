#if UNITY_EDITOR
using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace MenuCentralizer
{
    /// <summary>
    /// VRCExpressionsMenu を再帰的に読み取り、
    /// MA Menu Item の GameObject 階層として再現する。
    /// 差分更新: 既存 GameObject は保持し、不足分のみ追加する。
    /// </summary>
    public static class MenuExtractor
    {
        /// <summary>
        /// VRCExpressionsMenu を MA Menu Item 階層に変換する。
        /// </summary>
        /// <param name="menu">変換元の VRCExpressionsMenu。</param>
        /// <param name="parent">展開先の親 GameObject。</param>
        /// <returns>追加数とスキップ数のタプル。</returns>
        public static (int added, int skipped) Extract(VRCExpressionsMenu menu, GameObject parent)
        {
            if (menu == null) return (0, 0);

            int added = 0;
            int skipped = 0;
            var visited = new HashSet<VRCExpressionsMenu>();
            ExtractRecursive(menu, parent, visited, ref added, ref skipped);
            return (added, skipped);
        }

        // ------------------------------------------------------------------ //
        //  再帰処理
        // ------------------------------------------------------------------ //

        private static void ExtractRecursive(
            VRCExpressionsMenu menu,
            GameObject parent,
            HashSet<VRCExpressionsMenu> visited,
            ref int added,
            ref int skipped)
        {
            if (menu == null || visited.Contains(menu)) return;
            visited.Add(menu);

            foreach (var control in menu.controls)
            {
                if (control == null) continue;

                // --- 既存チェック（名前でマッチング） ---
                Transform existing = parent.transform.Find(control.name);

                if (existing != null)
                {
                    // 既存が SubMenu の場合は子を再帰的に差分更新
                    if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu
                        && control.subMenu != null)
                    {
                        ExtractRecursive(
                            control.subMenu, existing.gameObject,
                            visited, ref added, ref skipped);
                    }
                    skipped++;
                    continue;
                }

                // --- 新規 GameObject 生成 ---
                GameObject itemGO = new GameObject(control.name);
                Undo.RegisterCreatedObjectUndo(itemGO, "Create MA Menu Item");
                itemGO.transform.SetParent(parent.transform, false);

                // --- MA Menu Item コンポーネント設定 ---
                var maItem = itemGO.AddComponent<ModularAvatarMenuItem>();
                maItem.Control = new VRCExpressionsMenu.Control
                {
                    name  = control.name,
                    type  = control.type,
                    icon  = control.icon,
                    parameter = control.parameter != null
                        ? new VRCExpressionsMenu.Control.Parameter { name = control.parameter.name }
                        : new VRCExpressionsMenu.Control.Parameter { name = "" },
                    value = control.value,
                    style = control.style,
                };

                if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                {
                    maItem.MenuSource = SubmenuSource.Children;
                }

                CopySubLabels(control, maItem);
                CopySubParameters(control, maItem);

                // --- SubMenu の場合は子階層を再帰生成 ---
                if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu
                    && control.subMenu != null)
                {
                    ExtractRecursive(
                        control.subMenu, itemGO,
                        visited, ref added, ref skipped);
                }

                added++;
            }
        }

        // ------------------------------------------------------------------ //
        //  Control データのコピー
        // ------------------------------------------------------------------ //

        private static void CopySubLabels(
            VRCExpressionsMenu.Control source,
            ModularAvatarMenuItem target)
        {
            if (source.labels == null) return;

            target.Control.labels =
                new VRCExpressionsMenu.Control.Label[source.labels.Length];

            for (int i = 0; i < source.labels.Length; i++)
            {
                target.Control.labels[i] = new VRCExpressionsMenu.Control.Label
                {
                    name = source.labels[i].name,
                    icon = source.labels[i].icon,
                };
            }
        }

        private static void CopySubParameters(
            VRCExpressionsMenu.Control source,
            ModularAvatarMenuItem target)
        {
            if (source.subParameters == null) return;

            target.Control.subParameters =
                new VRCExpressionsMenu.Control.Parameter[source.subParameters.Length];

            for (int i = 0; i < source.subParameters.Length; i++)
            {
                target.Control.subParameters[i] =
                    new VRCExpressionsMenu.Control.Parameter
                    {
                        name = source.subParameters[i].name
                    };
            }
        }
    }
}
#endif
