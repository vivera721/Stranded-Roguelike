using UnityEngine;
using UnityEngine.SceneManagement;

namespace StrandedRoguelike
{
    public sealed class NextRoomPortal : MonoBehaviour
    {
        [SerializeField] private string nextSceneName;

        public void SetNextSceneName(string sceneName)
        {
            nextSceneName = sceneName;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                return;
            }

            if (!other.GetComponentInParent<PlayerMovement>())
            {
                return;
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }
}
