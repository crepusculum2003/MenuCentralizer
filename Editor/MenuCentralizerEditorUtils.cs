#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace MenuCentralizer
{
    /// <summary>
    /// MenuCentralizer 共通のエディタユーティリティ。
    /// </summary>
    public static class EditorUtils
    {
        // ------------------------------------------------------------------ //
        //  コンポーネント操作
        // ------------------------------------------------------------------ //

        /// <summary>
        /// 指定コンポーネントが未設定であれば追加して返す。
        /// </summary>
        public static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        /// <summary>
        /// parent 直下に name の子 GameObject を返す。なければ生成する。
        /// </summary>
        public static GameObject GetOrCreateChild(GameObject parent, string name)
        {
            Transform t = parent.transform.Find(name);
            if (t != null) return t.gameObject;

            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        // ------------------------------------------------------------------ //
        //  バリデーション
        // ------------------------------------------------------------------ //

        /// <summary>
        /// 選択中の GameObject から VRCAvatarDescriptor を取得する。
        /// 見つからない場合はエラーダイアログを表示して null を返す。
        /// </summary>
        public static VRCAvatarDescriptor TryGetAvatarDescriptor()
        {
            var avatarRoot = Selection.activeGameObject;
            if (avatarRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "エラー",
                    "Hierarchy でアバタールートを選択してください。",
                    "OK");
                return null;
            }

            var descriptor = avatarRoot.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null)
            {
                EditorUtility.DisplayDialog(
                    "エラー",
                    "選択オブジェクトに VRCAvatarDescriptor がありません。",
                    "OK");
                return null;
            }

            return descriptor;
        }

        /// <summary>
        /// 選択中の GameObject に VRCAvatarDescriptor が存在するか判定する。
        /// MenuItem バリデーション用。
        /// </summary>
        public static bool HasAvatarDescriptor()
        {
            if (Selection.activeGameObject == null) return false;
            return Selection.activeGameObject.GetComponent<VRCAvatarDescriptor>() != null;
        }
    }
}
#endif
