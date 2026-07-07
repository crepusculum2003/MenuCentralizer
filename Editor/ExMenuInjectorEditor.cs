#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MenuCentralizer
{
    /// <summary>
    /// ExMenuInjector のカスタムインスペクタ。
    /// コンポーネントの役割と注意事項を表示する。
    /// </summary>
    [CustomEditor(typeof(ExMenuInjector))]
    public class ExMenuInjectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Menu Centralizer ExMenu Injector が有効です。\n" +
                "ビルド時に Avatar Descriptor の ExpressionMenu を空にします。\n" +
                "これにより、非破壊的にメニューの差し替えが可能です。\n" +
                "何らかの理由により、Menu Installer と Avatar Descriptor の" +
                "メニューを共存させたい場合は\n" +
                "このコンポーネントを削除してください。",
                MessageType.Info);
        }
    }
}
#endif
