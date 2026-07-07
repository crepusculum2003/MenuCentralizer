#if UNITY_EDITOR
using nadena.dev.ndmf;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

[assembly: ExportsPlugin(typeof(MenuCentralizer.NdmfPlugin))]

namespace MenuCentralizer
{
    /// <summary>
    /// NDMF プラグイン: ビルド時に AvatarDescriptor の ExpressionMenu を
    /// 空のメニューに差し替える。MA より前のフェーズで実行される。
    /// </summary>
    public class NdmfPlugin : Plugin<NdmfPlugin>
    {
        public override string QualifiedName => "com.seal.menu-centralizer";
        public override string DisplayName  => "Menu Centralizer";

        protected override void Configure()
        {
            // MA より前のフェーズで実行する必要がある
            InPhase(BuildPhase.Transforming)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Replace default ExpressionMenu with empty", ctx =>
                {
                    var descriptor = ctx.AvatarRootObject
                        .GetComponent<VRCAvatarDescriptor>();
                    if (descriptor == null) return;

                    // ExMenuInjector が存在する場合のみ処理
                    var injector = ctx.AvatarRootObject
                        .GetComponentInChildren<ExMenuInjector>(true);
                    if (injector == null) return;

                    // ビルド専用の空メニューをメモリ上に作成（アセット保存なし）
                    var emptyMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                    descriptor.expressionsMenu = emptyMenu;
                });
        }
    }
}
#endif
