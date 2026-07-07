using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using VRC.SDKBase;

namespace MenuCentralizer
{
    /// <summary>
    /// このコンポーネントが存在するアバターに対して
    /// Menu Centralizer NDMF プラグインが動作します。
    /// ビルド時に AvatarDescriptor の ExpressionMenu を空に差し替え、
    /// MA Menu Item 階層をメインメニューとして使用します。
    /// </summary>
    [AddComponentMenu("Menu Centralizer/Menu Centralizer ExMenu Injector")]
    [DisallowMultipleComponent]
    [MovedFrom(false, null, null, "MenuCentralizer_ExMenuInjector")]
    public class ExMenuInjector : MonoBehaviour, IEditorOnly { }
}