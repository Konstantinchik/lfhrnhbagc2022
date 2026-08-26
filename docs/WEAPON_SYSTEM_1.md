# Система Оружия - Часть 1

## 📋 Обзор

Модульная система с 7 типами оружия, ScriptableObjects для настройки.

---

## 🏗️ Архитектура

### Компоненты
- **Weapon.cs** - главный компонент
- **WeaponSettingSO.cs** - настройки (ScriptableObject)
- **WeaponManager.cs** - менеджер (Singleton)

### Типы Оружия
```csharp
enum WeaponType {
    Pistol, Rifle, Shotgun, Sniper,
    RocketLauncher, Grenade, Melee
}
```

---

## 📦 Weapon Component

### Основные Поля
```csharp
public WeaponSettingSO weaponSetting;   // Настройки
public string weaponName;
public WeaponType weaponType;
public int ammoItemID;                  // ID патронов

public Transform muzzleFlashTransform;  // Вспышка
public Transform shellTransform;        // Гильзы

public float reloadAnimationDuration = 3.0f;
public bool autoReload = true;
public int maxAmmo;
public FireMode fireMode;               // automatic/single

public Item currentItem;                // Связь с инвентарём
```

---

## 🎮 WeaponSettingSO

### Создание
```
Right Click → Create → Weapon → Weapon Settings
```

### Параметры
```csharp
// Урон
public int damageMin = 10;
public int damageMax = 15;

// Стрельба
public float fireRate = 0.1f;
public float spread = 0.01f;
public int bulletsPerShot = 1;

// Звуки
public AudioClip shotSFX;
public AudioClip reloadingSFX;
public AudioClip emptySFX;

// Прицел
public bool useScope = false;
public float scopeFOV = 30f;
```

---

## 🔫 Механика Стрельбы

```csharp
public void Shot()
{
    // Проверки
    if (!canShot || reloading) return;
    if (currentAmmo <= 0) {
        if (autoReload) Reload();
        return;
    }
    
    // Raycast
    Vector3 origin = mainCamera.ViewportToWorldPoint(
        new Vector3(0.5f, 0.5f, 0)
    );
    Vector3 direction = CalculateSpreadDirection();
    
    RaycastHit hit;
    if (Physics.Raycast(origin, direction, out hit, 1000f))
        ProcessHit(hit);
    
    // Эффекты
    MuzzleFlashParticlesFX.Play();
    SpawnShell();
    audioSource.PlayOneShot(shotSFX);
    
    // Обновление
    currentAmmo--;
    UpdateAmmoUI();
}
```

### Обработка Попадания
```csharp
private void ProcessHit(RaycastHit hit)
{
    // Урон врагу
    if (hit.collider.CompareTag("Enemy")) {
        NPC npc = hit.collider.GetComponent<NPC>();
        if (npc != null)
            npc.ApplyDamage(Random.Range(damageMin, damageMax));
    }
    
    // Физика
    Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
    if (rb != null)
        rb.AddForceAtPosition(
            mainCamera.transform.forward * rigidbodyHitForce,
            hit.point,
            ForceMode.Impulse
        );
    
    // Декали
    SpawnDecal(hit);
}
```

---

## 🔄 Перезарядка

```csharp
public void Reload()
{
    if (reloading || currentAmmo >= maxAmmo) return;
    
    int availableAmmo = GetAmmoFromInventory();
    if (availableAmmo <= 0) return;
    
    reloading = true;
    canShot = false;
    
    animator.Play("Reload");
    audioSource.PlayOneShot(reloadingSFX);
    
    Invoke("ReloadComplete", reloadAnimationDuration);
}

private void ReloadComplete()
{
    int ammoNeeded = maxAmmo - currentAmmo;
    int ammoToAdd = Mathf.Min(ammoNeeded, GetAmmoFromInventory());
    
    currentAmmo += ammoToAdd;
    RemoveAmmoFromInventory(ammoToAdd);
    
    reloading = false;
    canShot = true;
}
```

---

**Продолжение:** [Часть 2 - Прицеливание и Эффекты](./WEAPON_SYSTEM_2.md)
