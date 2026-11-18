using UnityEngine;
using UnityEngine.UI;

namespace DarkTreeFPS
{
    /// <summary>Displays the currently active weapon name and ammunition.</summary>
    public sealed class WeaponHUDPresenter : MonoBehaviour
    {
        [SerializeField] private Text ammoText = null;
        [SerializeField] private Text weaponNameText = null;

        public void Show(string weaponName, string ammunition)
        {
            if (weaponNameText != null)
                weaponNameText.text = weaponName;

            if (ammoText != null)
            {
                ammoText.enabled = true;
                ammoText.text = ammunition;
            }
        }

        public void Clear()
        {
            if (weaponNameText != null)
                weaponNameText.text = string.Empty;

            if (ammoText != null)
                ammoText.text = string.Empty;
        }
    }
}
