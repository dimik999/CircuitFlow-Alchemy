using UnityEngine.SceneManagement;

namespace CircuitFlowAlchemy.Prototype.FactorioLite
{
    public static class SceneFlow
    {
        public const string MenuSceneName = "MainMenu";
        public const string GameSceneName = "SampleScene";

        public static int PendingLoadSlot = -1;
        public static bool PendingNewGame = false;

        public static bool IsMenuScene(string sceneName)
        {
            return sceneName == MenuSceneName;
        }

        public static bool IsGameplayScene(string sceneName)
        {
            return sceneName == GameSceneName;
        }

        public static void OpenMenuScene()
        {
            SceneManager.LoadScene(MenuSceneName);
        }

        public static void OpenGameSceneNew()
        {
            PendingNewGame = true;
            PendingLoadSlot = -1;
            SceneManager.LoadScene(GameSceneName);
        }

        public static void OpenGameSceneLoad(int slot)
        {
            PendingNewGame = false;
            PendingLoadSlot = slot;
            SceneManager.LoadScene(GameSceneName);
        }
    }
}
