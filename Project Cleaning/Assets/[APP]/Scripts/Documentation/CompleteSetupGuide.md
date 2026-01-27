# 🚀 Complete Setup Guide - Scene Transition & ContentSwitcher

## 📍 **1. SceneTransitionManager Setup**

### **Dimana Menaruh SceneTransitionManager:**

**Manual Setup di GamePlay Scene:**
```
Hierarchy:
├── Managers/
│   ├── GamePlayManager (sudah ada)
│   ├── UIManager (sudah ada)
│   └── SceneTransitionManager ← BUAT BARU
└── [Other GameObjects]
```

### **Steps:**
1. **Di GamePlay Scene** (scene tempat gameplay berlangsung):
   - Right-click di Hierarchy → Create Empty GameObject
   - Rename: `SceneTransitionManager`
   - Drag & drop script `SceneTransitionManager.cs` ke GameObject
   - ✅ Done! Script akan otomatis persist ke scene berikutnya

2. **Inspector Settings SceneTransitionManager:**
```
[Scene Configuration]
✅ Target Scene Name: "New Start Game Sandy"
✅ Scene Transition Delay: 0.5
✅ Use Easy Transition: true (atau false jika tidak pakai)

[Debug Settings]
✅ Enable Debug Logs: true
```

---

## 🖼️ **2. Finish Button Setup (Using Image)**

### **UIManager Inspector Setup:**

```
[UI Manager Components]
✅ Progress Dirts: (assign ProgressBar)
✅ Progress Dusts: (assign ProgressBar)
✅ Progress Assemble: (assign ProgressBar)
✅ Setting Canvas: (assign GameObject)
✅ Exit Button: (assign Button)
✅ Resume Button: (assign Button)
✅ Finish UI: (assign GameObject)
✅ Finish Background: (assign GameObject)
✅ Finish Button Image: ← DRAG IMAGE DISINI
✅ Use Image As Button: true
```

### **Finish Button Image Setup:**

**Hierarchy Structure:**
```
FinishUI/
├── Background
├── Text Elements
└── FinishButtonImage ← Drag ini ke UIManager.FinishButtonImage
```

**Requirements untuk Image:**
1. **Component Image** harus ada
2. **Raycast Target** = true (otomatis diset oleh script)
3. **Canvas Group** (optional untuk fade effects)

### **Alternatif Button Setup:**
Jika mau pakai Button component:
```
[UIManager Inspector]
✅ Use Image As Button: false
✅ Finish Button Image: (drag GameObject yang punya Button component)
```

---

## 🎮 **3. GamePlayManager Setup**

### **Inspector Configuration:**
```
[Object Type Configuration]
✅ Current Gameplay Object Type:
   - ChinaCoin (untuk gameplay coin China)
   - ChinaJar (untuk gameplay jar China)
   - IndonesiaKendin (untuk gameplay kendin Indonesia)
   - MesirWingedScared (untuk gameplay mesir)
✅ Auto Detect Object Type: true

[Scene Transition]
✅ Enable Scene Transition: true
✅ Transition Delay After Finish: 2.0 (detik delay sebelum pindah scene)
```

**Auto Detection Works:**
- Scene name mengandung "coin" → `ChinaCoin`
- Scene name mengandung "jar" → `ChinaJar`
- Scene name mengandung "kendin" → `IndonesiaKendin`
- Scene name mengandung "winged" atau "mesir" → `MesirWingedScared`

---

## 🎯 **4. ContentSwitcher Setup (New Start Game Sandy Scene)**

### **ContentSwitcher Inspector:**
```
[Chapter Configuration]
✅ Chapter Type: (pilih sesuai ObjectType yang akan diterima)
   - China (untuk ChinaCoin & ChinaJar)
   - Indonesia (untuk IndonesiaKendin)
   - Mesir (untuk MesirWingedScared)
✅ Object Type: (pilih sama dengan GamePlayManager)

[Testing Configuration]
✅ Current Testing Chapter: (sama dengan Chapter Type)
✅ Enable Testing Mode: false (untuk production)
```

---

## 🔄 **5. Complete Flow**

### **Execution Flow:**
```
1. Gameplay Scene loads
   ↓
2. GamePlayManager deteksi ObjectType (auto/manual)
   ↓
3. Player menyelesaikan gameplay (progress 100%)
   ↓
4. FinishUI muncul dengan FinishButtonImage
   ↓
5. Player klik FinishButtonImage
   ↓
6. UIManager.FinishButtonInteract() dipanggil
   ↓
7. SceneTransitionManager menyimpan ObjectType
   ↓
8. Scene transition ke "New Start Game Sandy"
   ↓
9. ContentSwitcher otomatis ter-trigger dengan ObjectType yang sesuai
   ↓
10. Animasi ContentSwitcher berjalan sesuai Chapter/ObjectType
```

---

## ✅ **6. Testing Checklist**

### **GamePlay Scene Testing:**
- [ ] SceneTransitionManager GameObject ada di scene
- [ ] GamePlayManager.currentGameplayObjectType sudah diset
- [ ] UIManager.FinishButtonImage sudah di-assign
- [ ] FinishButtonImage bisa diklik (raycastTarget = true)
- [ ] Console log muncul saat setup: "Finish button image listener added"

### **Runtime Testing:**
- [ ] Saat gameplay selesai, FinishUI muncul
- [ ] Klik FinishButtonImage memunculkan log: "=== FINISH BUTTON CLICKED ==="
- [ ] Scene transition terjadi setelah delay
- [ ] Console log: "=== TRIGGERING SCENE TRANSITION ==="

### **New Start Game Sandy Scene:**
- [ ] ContentSwitcher GameObject ada
- [ ] ContentSwitcher.chapterType match dengan ObjectType dari gameplay
- [ ] Console log: "=== CONTENT SWITCHER TRIGGERED SUCCESSFULLY ==="
- [ ] Animasi ContentSwitcher berjalan

---

## 🐛 **7. Troubleshooting**

### **Common Issues:**

**1. "Finish button image listener not added"**
- Solution: Pastikan FinishButtonImage di-assign di UIManager
- Check: useImageAsButton = true

**2. "Klik image tidak response"**
- Check: Image.raycastTarget = true
- Check: EventTrigger component otomatis ditambahkan
- Check: Canvas GraphicRaycaster component ada

**3. "SceneTransitionManager not found"**
- Solution: Buat GameObject SceneTransitionManager manual di scene
- Alternative: Script akan auto-create, tapi better manual setup

**4. "ContentSwitcher not triggered"**
- Check: Scene name "New Start Game Sandy" benar
- Check: ContentSwitcher.chapterType match dengan ObjectType
- Check: triggerButton di ContentSwitcher sudah diassign

**5. "Scene tidak pindah"**
- Check: enableSceneTransition = true di GamePlayManager
- Check: Scene "New Start Game Sandy" ada di Build Settings

---

## 📱 **8. Platform Support**

- ✅ **Windows/Mac/Linux**: Full support
- ✅ **Android/iOS**: Touch events supported
- ✅ **WebGL**: Mouse events supported
- ✅ **Unity Editor**: Testing supported

---

## 🎨 **9. Visual Effects**

### **Built-in Image Button Effects:**
- **Hover**: Image fades to 80% brightness
- **Click**: Image fades to 60% brightness
- **Disabled**: Image alpha = 50%

### **Customization:**
Modify `SetupFinishButtonHoverEffects()` in UIManager untuk custom effects.

---

## 🔧 **10. Manual Testing Methods**

### **Testing via Inspector:**
```csharp
// Test finish button
UIManager.Instance.TestFinishButton();

// Set ObjectType manual
GamePlayManager.Instance.SetObjectType(ObjectType.ChinaCoin);

// Immediate transition
GamePlayManager.Instance.TriggerImmediateTransition();
```

### **Testing via Console:**
Enable Debug Logs untuk detailed console output step-by-step.

**Setup Complete! 🎉**