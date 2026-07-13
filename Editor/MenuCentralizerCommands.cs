// =============================================================
//
//  Menu Centralizer — エディタコマンド
//
//  Hierarchy の右クリックメニュー（GameObject メニュー）から
//  呼び出される各コマンドのエントリポイント。
//
// =============================================================

#if UNITY_EDITOR
using System.Linq;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace MenuCentralizer
{
    /// <summary>
    /// MenuItem コマンド群。
    /// Hierarchy コンテキストメニューから各機能を実行する。
    /// </summary>
    public static class Commands
    {
        // ------------------------------------------------------------------ //
        //  定数
        // ------------------------------------------------------------------ //

        private const string MenuRootName    = "MC_ROOT";
        private const string MenuPath        = "GameObject/Menu Centralizer/Extract Menu to MA_Menu";
        private const string SyncPath        = "GameObject/Menu Centralizer/Sync Install Targets";
        private const string QuickSubmenuPath = "GameObject/Menu Centralizer/EASY Submenu Creation";
        private const int    CommandPriority = 9999;

        // ------------------------------------------------------------------ //
        //  Extract Menu
        // ------------------------------------------------------------------ //

        /// <summary>
        /// アバターの VRCExpressionsMenu を MA Menu Item 階層として抽出する。
        /// </summary>
        [MenuItem(MenuPath, false, CommandPriority)]
        public static void ExtractMenu()
        {
            // 環境チェック: Modular Avatar がインストールされているか
            bool maInstalled = System.AppDomain.CurrentDomain
                .GetAssemblies()
                .Any(a => a.GetName().Name == "nadena.dev.modular-avatar.core");

            if (!maInstalled)
            {
                EditorUtility.DisplayDialog(
                    "Modular Avatar が見つかりません",
                    "このツールには Modular Avatar が必要です。\n\n" +
                    "インストールリンク：https://modular-avatar.nadena.dev/ja",
                    "OK");
                return;
            }

            // アバター取得（バリデーション込み）
            var descriptor = EditorUtils.TryGetAvatarDescriptor();
            if (descriptor == null) return;

            var avatarRoot = descriptor.gameObject;
            VRCExpressionsMenu expressionsMenu = descriptor.expressionsMenu;

            if (expressionsMenu == null)
            {
                EditorUtility.DisplayDialog(
                    "エラー",
                    "VRCAvatarDescriptor に ExpressionMenu が設定されていません。",
                    "OK");
                return;
            }

            // --- 既に空メニューの場合: 抽出済みと判断して InstallTargets 同期のみ実行 ---
            bool menuIsEmpty = expressionsMenu.controls == null
                            || expressionsMenu.controls.Count == 0;

            if (menuIsEmpty)
            {
                var (syncAdded, syncSkipped) =
                    InstallTargetSynchronizer.Synchronize(avatarRoot);

                EditorUtility.DisplayDialog(
                    "スキップ",
                    "ExpressionMenu が空のため、抽出をスキップしました。\n\n" +
                    "【MA Install Targets 同期結果】\n" +
                    $"  追加: {syncAdded} 個\n" +
                    $"  スキップ（既存）: {syncSkipped} 個",
                    "OK");
                return;
            }

            // --- Undo 登録 ---
            Undo.RegisterFullObjectHierarchyUndo(avatarRoot, "Extract MA Menu");

            // --- 展開先 GameObject を取得または生成 ---
            GameObject menuRoot = EditorUtils.GetOrCreateChild(avatarRoot, MenuRootName);

            // ExMenuInjector を付与（NDMFプラグインの動作トリガー）
            EditorUtils.EnsureComponent<ExMenuInjector>(menuRoot);

            // ModularAvatarMenuInstaller を設定
            var installer = EditorUtils.EnsureComponent<ModularAvatarMenuInstaller>(menuRoot);
            installer.menuToAppend      = null; // 子 GameObject の MenuItem を使用
            installer.installTargetMenu = null; // null = アバタールートメニューへ直接追加

            // ModularAvatarMenuGroup をメニュールートにつける
            EditorUtils.EnsureComponent<ModularAvatarMenuGroup>(menuRoot);

            // --- 再帰的に差分抽出 ---
            var (added, skipped) = MenuExtractor.Extract(expressionsMenu, menuRoot);

            EditorUtility.SetDirty(avatarRoot);
            Debug.Log($"アバター Expression Menu 抽出完了: {added} 個追加 / {skipped} 個スキップ（既存）");

            // --- InstallTargets 同期 ---
            var (syncAddedStat, syncSkippedStat) =
                InstallTargetSynchronizer.Synchronize(avatarRoot);

            EditorUtility.DisplayDialog(
                "抽出完了",
                "MA_Menu への展開が完了しました。\n" +
                "【メニュー抽出結果】\n" +
                $"  追加: {added} 個\n" +
                $"  スキップ（既存）: {skipped} 個\n\n" +
                "【MA Install Targets 同期結果】\n" +
                $"  追加: {syncAddedStat} 個\n" +
                $"  スキップ（既存）: {syncSkippedStat} 個\n",
                "OK");
        }

        [MenuItem(MenuPath, true, CommandPriority)]
        private static bool ValidateExtractMenu() => EditorUtils.HasAvatarDescriptor();

        // ------------------------------------------------------------------ //
        //  Sync Install Targets
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Install Targets の同期のみを実行する。
        /// </summary>
        [MenuItem(SyncPath, false, CommandPriority + 10)]
        public static void SyncInstallTargets()
        {
            var descriptor = EditorUtils.TryGetAvatarDescriptor();
            if (descriptor == null) return;

            var (added, skippedCount) =
                InstallTargetSynchronizer.Synchronize(descriptor.gameObject);

            EditorUtility.DisplayDialog(
                "同期完了",
                "MA Install Targets の同期が完了しました。\n\n" +
                $"  追加: {added} 個\n" +
                $"  スキップ（既存）: {skippedCount} 個",
                "OK");
        }

        [MenuItem(SyncPath, true)]
        private static bool ValidateSyncInstallTargets() => EditorUtils.HasAvatarDescriptor();

        // ------------------------------------------------------------------ //
        //  Quick Submenu Creation
        // ------------------------------------------------------------------ //

        /// <summary>
        /// ワンボタンで MA Menu Item SubMenu が設定された GameObject を作成する。
        /// </summary>
        [MenuItem(QuickSubmenuPath, false, CommandPriority + 20)]
        public static void QuickAddSubmenu()
        {
            const string defaultName = "Submenu";
            var parent = Selection.activeGameObject;

            GameObject go = new GameObject(defaultName);
            Undo.RegisterCreatedObjectUndo(go, $"Create {defaultName}");
            go.transform.SetParent(parent.transform, false);

            // MA Menu Item 設定
            var maItem = go.AddComponent<ModularAvatarMenuItem>();
            maItem.Control.name = defaultName;
            maItem.Control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
            maItem.MenuSource   = SubmenuSource.Children;
        }
    }
}
#endif
