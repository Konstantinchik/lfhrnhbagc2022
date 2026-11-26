using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkTreeFPS.Tests
{
    /// <summary>
    /// Persistent event-driven test harness for save loading (F9) on the scene's Dummy.
    /// It waits for a real SaveData load and then compares the restored inventory with the
    /// saved file and checks the grid bookkeeping. It never saves, loads or changes items itself.
    /// </summary>
    public sealed class SaveLoadInventoryDebugTest : MonoBehaviour
    {
#if UNITY_EDITOR
        // The scene is reloaded on load, so a new instance of this component has to pick up the pending check.
        private static bool loadPending;
        private static int loadsChecked;

        private void Start()
        {
            Debug.Log("[Save load test] TEST START. Press F5 to save and F9 to load; each load is checked. Console file: " +
                UnityConsoleFileCapture.LogPath, this);
            if (!string.IsNullOrEmpty(UnityConsoleFileCapture.CaptureFailure))
                Debug.LogError("[Save load test] Console file capture failed: " +
                    UnityConsoleFileCapture.CaptureFailure, this);
        }

        private void LateUpdate()
        {
            if (DTInventory.SaveData.loadDataTrigger)
            {
                loadPending = true;
                return;
            }

            if (!loadPending)
                return;

            loadPending = false;
            StartCoroutine(CheckAfterLoad());
        }

        private IEnumerator CheckAfterLoad()
        {
            // Destroy() calls made by the load are applied at the end of the frame.
            yield return null;
            yield return null;

            loadsChecked++;
            var problems = new List<string>();
            var inventory = FindObjectOfType<DTInventory.DTInventory>();

            if (inventory == null)
            {
                Report(problems, "DTInventory not found after load", 0, 0);
                yield break;
            }

            string path = Application.persistentDataPath + "/" + SceneManager.GetActiveScene().name + "_inventoryData";
            if (!File.Exists(path))
            {
                problems.Add("save file not found: " + path);
                Report(problems, null, 0, inventory.inventoryItems.Count);
                yield break;
            }

            var saved = JsonUtility.FromJson<DTInventory.InventoryData>(File.ReadAllText(path));
            int savedCount = saved.itemNames != null ? saved.itemNames.Length : 0;

            // 1. No stale entries left from the previous session state.
            foreach (var inventoryItem in inventory.inventoryItems)
            {
                if (inventoryItem == null || inventoryItem.item == null)
                    problems.Add("inventoryItems contains a destroyed entry");
            }

            // 2. Every saved item is back in its saved cell with its saved stack size.
            for (int i = 0; i < savedCount; i++)
            {
                int x = (int)saved.itemGridPos[i].x;
                int y = (int)saved.itemGridPos[i].y;
                bool found = false;

                foreach (var inventoryItem in inventory.inventoryItems)
                {
                    if (inventoryItem == null || inventoryItem.item == null)
                        continue;

                    if (inventoryItem.x == x && inventoryItem.y == y && inventoryItem.item.title == saved.itemNames[i])
                    {
                        found = true;
                        if (inventoryItem.item.stackSize != saved.stackSize[i])
                            problems.Add("stack size mismatch for " + saved.itemNames[i] + " at " + x + "," + y +
                                ": saved " + saved.stackSize[i] + ", restored " + inventoryItem.item.stackSize);
                        break;
                    }
                }

                if (!found)
                    problems.Add("saved item not restored: " + saved.itemNames[i] + " at " + x + "," + y);
            }

            if (inventory.inventoryItems.Count != savedCount)
                problems.Add("item count mismatch: saved " + savedCount + ", restored " + inventory.inventoryItems.Count);

            // 3. Occupied cells match the restored items exactly.
            var occupied = new HashSet<long>();
            foreach (var inventoryItem in inventory.inventoryItems)
            {
                if (inventoryItem == null)
                    continue;

                for (int x = inventoryItem.x; x < inventoryItem.x + inventoryItem.width; x++)
                    for (int y = inventoryItem.y; y < inventoryItem.y + inventoryItem.height; y++)
                        occupied.Add(((long)x << 32) | (uint)y);
            }

            int staleCells = 0;
            int unmarkedCells = 0;
            foreach (var slot in inventory.slots)
            {
                if (slot == null)
                    continue;

                bool shouldBeOccupied = occupied.Contains(((long)slot.x << 32) | (uint)slot.y);
                if (!slot.free && !shouldBeOccupied)
                    staleCells++;
                if (slot.free && shouldBeOccupied)
                    unmarkedCells++;
            }

            if (staleCells > 0)
                problems.Add(staleCells + " cell(s) marked occupied without an item");
            if (unmarkedCells > 0)
                problems.Add(unmarkedCells + " cell(s) under an item are marked free");

            // 4. No orphaned item objects left under the inventory.
            int storedItemObjects = inventory.GetComponentsInChildren<DTInventory.Item>(true).Length;
            if (storedItemObjects != inventory.inventoryItems.Count)
                problems.Add("item objects under inventory: " + storedItemObjects +
                    ", expected " + inventory.inventoryItems.Count);

            Report(problems, null, savedCount, inventory.inventoryItems.Count);
        }

        private void Report(List<string> problems, string fatal, int savedCount, int restoredCount)
        {
            if (fatal != null)
                problems.Add(fatal);

            if (problems.Count == 0)
            {
                Debug.Log("[Save load test] PASS: load #" + loadsChecked + " saved=" + savedCount +
                    " restored=" + restoredCount + ".", this);
                Debug.Log("[Save load test] TEST END: PASS.", this);
                return;
            }

            Debug.LogError("[Save load test] FAIL: load #" + loadsChecked + " saved=" + savedCount +
                " restored=" + restoredCount + ". " + string.Join("; ", problems.ToArray()), this);
            Debug.LogError("[Save load test] TEST END: FAIL.", this);
        }
#endif
    }
}
