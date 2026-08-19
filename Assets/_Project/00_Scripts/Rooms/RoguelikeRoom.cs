using System.Collections.Generic;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class RoguelikeRoom : MonoBehaviour
    {
        [SerializeField] private List<GameObject> doors = new List<GameObject>();
        [SerializeField] private List<EnemyHealth> enemies = new List<EnemyHealth>();
        [SerializeField] private GameObject nextRoomPortal;
        [SerializeField] private bool closeDoorsOnEnter = true;
        [SerializeField] private bool openWhenNoEnemies = true;

        private bool started;
        private bool cleared;

        private void Awake()
        {
            if (nextRoomPortal != null)
            {
                nextRoomPortal.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (started || !other.GetComponentInParent<PlayerMovement>())
            {
                return;
            }

            started = true;

            if (closeDoorsOnEnter)
            {
                SetDoorsActive(true);
            }
        }

        private void Update()
        {
            if (!started || cleared || !openWhenNoEnemies)
            {
                return;
            }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] == null)
                {
                    enemies.RemoveAt(i);
                }
            }

            if (enemies.Count == 0)
            {
                ClearRoom();
            }
        }

        public void RegisterEnemy(EnemyHealth enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
            {
                enemies.Add(enemy);
            }
        }

        public void RegisterDoor(GameObject door)
        {
            if (door != null && !doors.Contains(door))
            {
                doors.Add(door);
            }
        }

        public void SetNextRoomPortal(GameObject portal)
        {
            nextRoomPortal = portal;

            if (nextRoomPortal != null)
            {
                nextRoomPortal.SetActive(false);
            }
        }

        private void ClearRoom()
        {
            cleared = true;
            SetDoorsActive(false);

            if (nextRoomPortal != null)
            {
                nextRoomPortal.SetActive(true);
            }
        }

        private void SetDoorsActive(bool active)
        {
            for (int i = 0; i < doors.Count; i++)
            {
                if (doors[i] != null)
                {
                    doors[i].SetActive(active);
                }
            }
        }
    }
}
