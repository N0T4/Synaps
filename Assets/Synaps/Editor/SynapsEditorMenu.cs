using UnityEditor;
using UnityEditor.SceneManagement;

namespace Synaps.Editor
{
    public static class SynapsEditorMenu
    {
        [MenuItem("Synaps/Open Desktop Prototype")]
        public static void OpenDesktopPrototype()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/Synaps/Scenes/DesktopPrototype.unity");
        }
    }
}
