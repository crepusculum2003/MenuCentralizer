#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;

namespace MenuCentralizer
{
    /// <summary>
    /// アバター内の ModularAvatarMenuInstaller を検出し、
    /// MC_ROOT 配下に対応する ModularAvatarMenuInstallTarget を同期生成する。
    /// </summary>
    public static class InstallTargetSynchronizer
    {
        /// <summary>
        /// Install Targets を同期し、(追加数, スキップ数) を返す。
        /// </summary>
        public static (int added, int skipped) Synchronize(GameObject avatarRoot)
        {
            int added = 0;
            int skipped = 0;

            // --- MenuGroup の検出 ---
            var menuGroup = avatarRoot
                .GetComponentsInChildren<ModularAvatarMenuGroup>(true)
                .FirstOrDefault();

            if (menuGroup == null)
            {
                Debug.LogError("MA Menu Group not found.");
                return (added, skipped);
            }

            var menuRoot = menuGroup.gameObject;

            // --- MC_ROOT 外の有効な Installer を収集 ---
            var installers = avatarRoot
                .GetComponentsInChildren<ModularAvatarMenuInstaller>(true)
                .Where(installer =>
                {
                    // MC_ROOT の子であればスキップ
                    if (installer.transform.IsChildOf(menuRoot.transform))
                        return false;

                    // 無効ならスキップ
                    if (!installer.enabled)
                        return false;

                    return true;
                })
                .ToArray();

            // --- internal 型を Reflection で取得 ---
            var installTargetType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(
                    "nadena.dev.modular_avatar.core.ModularAvatarMenuInstallTarget"))
                .FirstOrDefault(t => t != null);

            if (installTargetType == null)
            {
                Debug.LogError("ModularAvatarMenuInstallTarget type not found.");
                return (added, skipped);
            }

            var installerField = installTargetType.GetField(
                "installer",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // --- 各 Installer に対して InstallTarget を生成 ---
            foreach (var installer in installers)
            {
                // 重複生成防止
                bool alreadyExists = menuRoot
                    .GetComponentsInChildren<Component>(true)
                    .Any(c =>
                    {
                        if (c == null || c.GetType() != installTargetType)
                            return false;
                        return ReferenceEquals(installerField.GetValue(c), installer);
                    });

                if (alreadyExists)
                {
                    skipped++;
                    continue;
                }

                Undo.IncrementCurrentGroup();

                var go = new GameObject(installer.name + " Install Target");
                Undo.RegisterCreatedObjectUndo(go, "Create MA Install Target");
                go.transform.SetParent(menuRoot.transform, false);

                var component = Undo.AddComponent(go, installTargetType);
                installerField.SetValue(component, installer);

                EditorUtility.SetDirty(go);
                added++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Finished creating MA Menu Install Targets. added={added} / skipped={skipped}");

            return (added, skipped);
        }
    }
}
#endif
