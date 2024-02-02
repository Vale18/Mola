using UnityEngine;
using UnityEngine.SceneManagement;  // Required for scene management

public class GameStarter : MonoBehaviour
{
    // Name of the scene to load when the game starts
    public string gameSceneName = "GameScene";  // Change this to the name of your main game scene

    private bool gameStarted = false;

    void Update()
    {
        // Check for space key press and if the game hasn't started yet
        if (Input.GetKeyDown(KeyCode.Return) && !gameStarted)
        {
            StartGame();
        }
    }

    void StartGame()
    {
        gameStarted = true;

        // Load the main game scene
        SceneManager.LoadScene(gameSceneName);
    }
}
