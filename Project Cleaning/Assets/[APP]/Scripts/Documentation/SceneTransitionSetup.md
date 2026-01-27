# Scene Transition & ContentSwitcher System Setup Guide

## Overview
Sistem ini memungkinkan untuk mengirim `ObjectType` dari GamePlay scene ke ContentSwitcher di scene "New Start Game Sandy" melalui finish button.

## 🔧 Setup Requirements

### 1. Inspector Setup

#### GamePlayManager.cs
```
[Object Type Configuration]
✅ Current Gameplay Object Type: ChinaCoin/ChinaJar/IndonesiaKendin/MesirWingedScared
✅ Auto Detect Object Type: true (otomatis deteksi dari nama scene)

[Scene Transition]
✅ Enable Scene Transition: true
✅ Transition Delay After Finish: 2.0 (detik delay sebelum pindah scene)
```

#### UIManager.cs
```
✅ Finish Button: Drag finish button dari FinishUI ke field ini
```

#### SceneTransitionManager (auto-created)
```
[Scene Configuration]
✅ Target Scene Name: "New Start Game Sandy"
✅ Scene Transition Delay: 0.5
✅ Use Easy Transition: true

[Debug Settings]
✅ Enable Debug Logs: true (untuk testing)
```

#### ContentSwitcher.cs (di scene New Start Game Sandy)
```
[Chapter Configuration]
✅ Chapter Type: China/Indonesia/Mesir (sesuai dengan objektype yang akan diterima)
✅ Object Type: ChinaCoin/ChinaJar/IndonesiaKendin/MesirWingedScared
```

### 2. Scene Setup

#### GamePlay Scene:
- ✅ GamePlayManager ada di scene
- ✅ UIManager ada dan finish button sudah diassign
- ✅ SceneTransitionManager akan auto-created saat dibutuhkan

#### New Start Game Sandy Scene:
- ✅ ContentSwitcher ada di scene
- ✅ ContentSwitcher sudah disetup dengan trigger button

### 3. Hierarchy Setup

```
GamePlay Scene:
├── GamePlayManager (Script: GamePlayManager.cs)
├── UICanvas
│   ├── FinishUI
│   │   └── FinishButton ← Harus diassign ke UIManager
│   └── UIManager (Script: UIManager.cs)
└── [Other GamePlay Objects]

New Start Game Sandy Scene:
├── ContentSwitcher GameObject (Script: ContentSwitcher.cs)
├── [Content UI Elements]
└── [Other Scene Objects]
```

## 🚀 Flow Execution

### Step 1: Gameplay Selesai
```
GamePlayManager.FinishedGame()
↓
Progress >= 100%
↓
UIManager.ShowFinishUI(true) ← FinishButton muncul
```

### Step 2: User Click Finish Button
```
FinishButton.onClick
↓
UIManager.FinishButtonInteract()
↓
GamePlayManager.TriggerFinishButton()
↓
SceneTransitionManager.TransitionToMainSceneWithContentSwitcher(ObjectType)
```

### Step 3: Scene Transition
```
SceneTransitionManager menyimpan ObjectType
↓
Load scene "New Start Game Sandy"
↓
OnSceneLoaded() triggered
↓
TriggerContentSwitcherAfterDelay()
```

### Step 4: ContentSwitcher Activation
```
Find ContentSwitcher yang match dengan ChapterType
↓
Set ObjectType yang sesuai
↓
Trigger ContentSwitcher.OnButtonClicked()
↓
ContentSwitcher menjalankan animasi sesuai ObjectType
```

## 📝 Usage Examples

### Auto-Detection (Recommended)
```csharp
// Set autoDetectObjectType = true di GamePlayManager
// Sistem akan deteksi ObjectType dari nama scene:

// Scene "ChinaCoin_Gameplay" → ObjectType.ChinaCoin
// Scene "Jar_Cleaning" → ObjectType.ChinaJar
// Scene "Indonesia_Kendin" → ObjectType.IndonesiaKendin
// Scene "Mesir_WingedScared" → ObjectType.MesirWingedScared
```

### Manual Setting
```csharp
// Set ObjectType secara manual
GamePlayManager.Instance.SetObjectType(ObjectType.ChinaCoin);

// Trigger transition manual
GamePlayManager.Instance.TriggerFinishButton();

// Immediate transition (tidak wait delay)
GamePlayManager.Instance.TriggerImmediateTransition();
```

### Testing Methods
```csharp
// Test scene transition dari script lain
SceneTransitionManager.Instance.TransitionToMainSceneWithContentSwitcher(ObjectType.IndonesiaKendin);

// Check current data
ObjectType currentType = SceneTransitionManager.Instance.GetCurrentObjectType();
ChapterType currentChapter = SceneTransitionManager.Instance.GetCurrentChapterType();
```

## 🐛 Debug Information

### Console Logs Sequence:
```
=== GAMEPLAY MANAGER INITIALIZED ===
Current Object Type: ChinaCoin
Scene Transition Enabled: True

=== FINISH BUTTON CLICKED ===
=== GAME FINISHED - Starting transition in 2 seconds ===
=== TRIGGERING SCENE TRANSITION ===
Object Type: ChinaCoin
Target Scene: New Start Game Sandy

=== SCENE TRANSITION MANAGER ===
Object Type Set: ChinaCoin
Chapter Type: China
Will trigger ContentSwitcher: True

Scene loaded: New Start Game Sandy
=== TRIGGERING CONTENT SWITCHER ===
Looking for ContentSwitcher with ObjectType: ChinaCoin
Found 1 ContentSwitcher(s) in scene
Triggered ContentSwitcher: ContentSwitcher_China
Chapter: China, Object: ChinaCoin
=== CONTENT SWITCHER TRIGGERED SUCCESSFULLY ===
```

## ⚠️ Troubleshooting

### Common Issues:

1. **"SceneTransitionManager not found"**
   - Solution: Sistem akan auto-create, tapi pastikan SceneTransitionManager.cs ada di project

2. **"Failed to find or trigger ContentSwitcher"**
   - Check: ContentSwitcher ada di scene "New Start Game Sandy"?
   - Check: ChapterType di ContentSwitcher sudah benar?

3. **"Finish button not assigned"**
   - Solution: Drag finish button dari FinishUI ke UIManager.FinishButton field

4. **Scene tidak pindah**
   - Check: enableSceneTransition = true di GamePlayManager?
   - Check: Scene "New Start Game Sandy" ada di Build Settings?

5. **ContentSwitcher tidak trigger**
   - Check: ContentSwitcher.triggerButton sudah diassign?
   - Check: ChapterType match dengan ObjectType yang dikirim?

### Debug Steps:
1. Enable debug logs di SceneTransitionManager
2. Check console logs untuk error messages
3. Verify object assignments di Inspector
4. Test dengan manual trigger methods

## 📱 Platform Notes

- ✅ Works on Android/iOS
- ✅ Works in Unity Editor
- ✅ Compatible dengan EasyTransition package
- ✅ Fallback ke standard Unity scene loading jika EasyTransition tidak ada

## 🔄 System Dependencies

- ContentSwitcher.cs
- GamePlayManager.cs
- UIManager.cs
- SceneTransitionManager.cs (new)
- ObjectType enum
- ChapterType enum