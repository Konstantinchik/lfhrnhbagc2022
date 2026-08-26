# Система Оружия - Часть 2

## 🎯 Прицеливание

```csharp
public void SetAim(bool aiming)
{
    setAim = aiming;
    
    if (aiming && useScope) {
        // Включить прицел
        scopeUI.SetActive(true);
        mainCamera.fieldOfView = scopeFOV;
        controller.sensitivity *= aimSensitivityMultiplier;
    }
    else {
        // Выключить
        scopeUI.SetActive(false);
        mainCamera.fieldOfView = normalMainCameraFOV;
        controller.sensitivity = normalSensitivity;
    }
}
```

---

## ⚙️ WeaponManager

```csharp
public class WeaponManager : MonoBehaviour
{
    public List<Weapon> weapons;
    public Weapon activeWeapon;
    
    public void EquipWeapon(Item item)
    {
        Weapon weaponToEquip = weapons.Find(
            w => w.weaponName == item.title
        );
        
        if (activeWeapon != null)
            HideWeapon();
        
        weaponToEquip.gameObject.SetActive(true);
        activeWeapon = weaponToEquip;
    }
}
```

---

## 🎨 Визуальные Эффекты

### Muzzle Flash
```csharp
private void PlayMuzzleFlash()
{
    if (MuzzleFlashParticlesFX != null)
        MuzzleFlashParticlesFX.Play();
}
```

### Shell Ejection (Object Pooling)
```csharp
private void SpawnShell()
{
    GameObject shellObj = shells[shellIndex];
    shellIndex = (shellIndex + 1) % shellPoolSize;
    
    shellObj.transform.position = shellTransform.position;
    shellObj.SetActive(true);
    
    Rigidbody rb = shellObj.GetComponent<Rigidbody>();
    rb.AddForce(
        shellTransform.right * shellForce,
        ForceMode.Impulse
    );
}
```

### Bullet Decals
```csharp
private void SpawnDecal(RaycastHit hit)
{
    GameObject[] decalArray = GetDecalArrayByTag(hit.collider.tag);
    int index = Random.Range(0, decalArray.Length);
    
    GameObject decal = Instantiate(
        decalArray[index],
        hit.point + hit.normal * 0.001f,
        Quaternion.LookRotation(hit.normal)
    );
    
    Destroy(decal, 10f);
}
```

---

## 📝 Создание Нового Оружия

### Пример: AK-47

**1. WeaponSettingSO:**
```
Right Click → Create → Weapon → Weapon Settings
Имя: AK-47_Settings
```

**Параметры:**
```csharp
weaponName = "AK-47";
weaponType = Rifle;
damageMin = 25;
damageMax = 35;
fireRate = 0.1f;
spread = 0.02f;
```

**2. Префаб:**
- Модель в: Weapon holder → Sway Transform
- Добавить: компонент Weapon
- Назначить: weaponSetting = AK-47_Settings
- Настроить Animator

**3. Item:**
```
Right Click → Create → Inventory → Item
```
```csharp
title = "AK-47";
itemType = Equipment;
equipmentType = PrimaryWeapon;
```

**4. Связать:**
```csharp
Weapon.currentItem = Item (AK-47)
Weapon.ammoItemID = 1
```

---

## 🚀 Специальные Типы

### Shotgun
```csharp
// WeaponSettingSO:
bulletsPerShot = 8;
spread = 0.05f;

// Shot():
for (int i = 0; i < bulletsPerShot; i++) {
    Vector3 direction = CalculateSpreadDirection();
    Physics.Raycast(origin, direction, out hit);
}
```

### Rocket Launcher
```csharp
private void FireRocket()
{
    GameObject rocket = Instantiate(
        projectilePrefab,
        muzzleFlashTransform.position,
        muzzleFlashTransform.rotation
    );
    
    rocket.GetComponent<Rigidbody>().velocity = 
        muzzleFlashTransform.forward * projectileSpeed;
    
    MissileObject.SetActive(false);
    Invoke("ShowMissileObject", timeToShowMissileObject);
}
```

### Grenade
```csharp
public void ThrowGrenade()
{
    DTFPSInventoryExtended.UseGrenade(inventory);
    
    GameObject grenade = Instantiate(
        grenadePrefab,
        muzzleFlashTransform.position,
        Quaternion.identity
    );
    
    grenade.GetComponent<Rigidbody>().AddForce(
        (mainCamera.transform.forward + Vector3.up * 0.3f) * throwForce
    );
}
```

---

## 🐛 Troubleshooting

### Оружие не стреляет
- ✅ canShot = true?
- ✅ reloading = false?
- ✅ currentAmmo > 0?
- ✅ WeaponSettingSO назначен?

### Нет урона
- ✅ Теги (Enemy, Player)?
- ✅ Компоненты (NPC, PlayerStats)?
- ✅ LayerMask для Raycast?

### Нет эффектов
- ✅ Префабы назначены?
- ✅ Transform точки правильные?
- ✅ ParticleSystem активен?

---

**Назад:** [Часть 1](./WEAPON_SYSTEM_1.md)  
**Далее:** [Система Инвентаря](./INVENTORY_SYSTEM.md)
